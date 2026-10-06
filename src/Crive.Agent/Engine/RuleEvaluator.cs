using Crive.Shared.DTOs;
using Crive.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Crive.Agent.Engine;

/// <summary>
/// Motor de avaliação de regras de bloqueio de domínios.
/// Implementa correspondência exata de domínio + subdomínios com hierarquia de prioridade.
/// Thread-safe para hot-reload de regras via SignalR.
/// </summary>
public sealed class RuleEvaluator
{
    private readonly ILogger<RuleEvaluator> _logger;
    private readonly ReaderWriterLockSlim _lock = new();

    // Mapa de domínio base -> lista de regras ordenadas por prioridade
    // Chaves são domínios normalizados em lowercase sem trailing dot
    private Dictionary<string, List<CompiledRuleDto>> _domainRules = new(StringComparer.OrdinalIgnoreCase);
    private string _currentRuleHash = string.Empty;

    public string CurrentRuleHash
    {
        get
        {
            _lock.EnterReadLock();
            try { return _currentRuleHash; }
            finally { _lock.ExitReadLock(); }
        }
    }

    public RuleEvaluator(ILogger<RuleEvaluator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Carrega (ou recarrega) o pacote de regras compiladas recebido do servidor.
    /// Reconstrói o índice de domínios para avaliação rápida.
    /// </summary>
    public void LoadRules(RulePackageDto package)
    {
        _lock.EnterWriteLock();
        try
        {
            var newRules = new Dictionary<string, List<CompiledRuleDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in package.Rules)
            {
                // Ignorar regras temporárias expiradas
                if (rule.IsTemporary && rule.ExpiresAt.HasValue && rule.ExpiresAt.Value <= DateTime.UtcNow)
                    continue;

                foreach (var domain in rule.Domains)
                {
                    var normalizedDomain = NormalizeDomain(domain);
                    if (string.IsNullOrEmpty(normalizedDomain))
                        continue;

                    if (!newRules.TryGetValue(normalizedDomain, out var list))
                    {
                        list = new List<CompiledRuleDto>();
                        newRules[normalizedDomain] = list;
                    }
                    list.Add(rule);
                }
            }

            // Ordenar cada lista por prioridade (menor número = maior precedência)
            // Em caso de empate: BLOCK tem precedência sobre ALLOW
            foreach (var list in newRules.Values)
            {
                list.Sort((a, b) =>
                {
                    int priorityCompare = a.Priority.CompareTo(b.Priority);
                    if (priorityCompare != 0) return priorityCompare;
                    // BLOCK (0) antes de ALLOW (1)
                    return a.Action.CompareTo(b.Action);
                });
            }

            _domainRules = newRules;
            _currentRuleHash = package.RuleHash;

            _logger.LogInformation(
                "Regras carregadas: {RuleCount} regras, {DomainCount} domínios únicos, hash={Hash}",
                package.Rules.Count, newRules.Count, package.RuleHash);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Avalia se um domínio deve ser bloqueado com base nas regras carregadas.
    /// 
    /// Algoritmo de correspondência (domínio exato + subdomínios):
    /// - Regra para "youtube.com" bloqueia: youtube.com, www.youtube.com, m.youtube.com
    /// - Regra para "youtube.com" NÃO bloqueia: notyoutube.com, foryou.com, youtubedownloader.net
    /// 
    /// Hierarquia de resolução:
    /// 1. Procura regra MACHINE (priority ~100) — maior precedência
    /// 2. Procura regra SECTOR (priority ~500)
    /// 3. Procura regra GLOBAL (priority ~1000) — menor precedência
    /// 4. Se nenhuma regra, PERMITE (default-allow)
    /// </summary>
    public EvaluationResult Evaluate(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return EvaluationResult.Allowed;

        domain = NormalizeDomain(domain);
        if (string.IsNullOrEmpty(domain))
            return EvaluationResult.Allowed;

        _lock.EnterReadLock();
        try
        {
            // Percorrer o domínio e cada domínio pai (strip do label mais à esquerda)
            // Ex: "video.m.youtube.com" → verifica:
            //   1. "video.m.youtube.com"
            //   2. "m.youtube.com"
            //   3. "youtube.com"
            //   4. "com" (improvável ter regra, mas correto)
            var labels = domain.Split('.');

            for (int i = 0; i < labels.Length; i++)
            {
                // Construir domínio candidato a partir do label atual até o final
                var candidateDomain = string.Join('.', labels, i, labels.Length - i);

                if (_domainRules.TryGetValue(candidateDomain, out var rules))
                {
                    // Encontrar a primeira regra ativa (já ordenadas por prioridade)
                    foreach (var rule in rules)
                    {
                        // Verificar expiração de regras temporárias
                        if (rule.IsTemporary && rule.ExpiresAt.HasValue && rule.ExpiresAt.Value <= DateTime.UtcNow)
                            continue;

                        return new EvaluationResult(
                            IsBlocked: rule.Action == RuleAction.Block,
                            MatchedRule: rule,
                            Action: rule.Action,
                            MatchedDomain: candidateDomain);
                    }
                }
            }

            // Nenhuma regra encontrada — default: permitir
            return EvaluationResult.Allowed;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Normaliza um domínio: lowercase, remove trailing dot, remove wildcard prefix.
    /// </summary>
    private static string NormalizeDomain(string domain)
    {
        domain = domain.Trim().ToLowerInvariant();

        // Remover trailing dot (FQDN format)
        domain = domain.TrimEnd('.');

        // Remover prefixo wildcard "*.": trata "*.youtube.com" como "youtube.com"
        // (a correspondência de subdomínios é feita pelo algoritmo de busca)
        if (domain.StartsWith("*."))
            domain = domain[2..];

        // Remover protocolo se presente por engano
        if (domain.StartsWith("http://"))
            domain = domain[7..];
        if (domain.StartsWith("https://"))
            domain = domain[8..];

        // Remover path/query se presente
        var slashIndex = domain.IndexOf('/');
        if (slashIndex >= 0)
            domain = domain[..slashIndex];

        return domain;
    }
}

/// <summary>
/// Resultado da avaliação de um domínio contra as regras de bloqueio.
/// </summary>
public record EvaluationResult(
    bool IsBlocked,
    CompiledRuleDto? MatchedRule,
    RuleAction Action,
    string? MatchedDomain = null)
{
    /// <summary>Resultado padrão: domínio permitido, sem regra correspondente.</summary>
    public static readonly EvaluationResult Allowed = new(false, null, RuleAction.Allow);
}

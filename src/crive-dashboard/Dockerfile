# Etapa 1: Build do Node.js
FROM node:20-alpine AS build
WORKDIR /app

# Instalar dependências
COPY package*.json ./
RUN npm install

# Copiar o código e fazer o build para produção
COPY . .
RUN npx vite build

# Etapa 2: Servidor Nginx para rodar o build
FROM nginx:alpine
# Copiar os arquivos estáticos gerados pelo Vite
COPY --from=build /app/dist /usr/share/nginx/html

# Copiar a configuração customizada do Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

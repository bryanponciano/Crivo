namespace Crive.Agent.Protection;

public class SelfProtection
{
    public void Enable()
    {
        // Set restrictive ACLs on service to prevent stop/disable
    }

    public void Apply()
    {
        Enable();
    }
}

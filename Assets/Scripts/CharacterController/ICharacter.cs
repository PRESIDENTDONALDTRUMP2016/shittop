namespace Invector{    
    public interface ICharacter{
        void ResetRagdoll();
        void EnableRagdoll();
        void RagdollGettingUp();
        float startingHealth { get; }
        float currentHealth { get; set; }
        bool ragdolled { get; set; }
    }
}
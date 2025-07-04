using UnityEngine;

public interface IEffect
{
    /// <summary>
    /// Applies this effect. User and target are Entities for maximum flexibility and type safety.
    /// </summary>
    /// <param name="user">The entity causing the effect (e.g., Vehicle, Obstacle, etc.)</param>
    /// <param name="target">The entity receiving the effect (e.g., Vehicle, Obstacle, etc.)</param>
    /// <param name="context">Optional context, such as the current stage or other relevant object.</param>
    /// <param name="source">The source of the effect (e.g., Skill, EventCard, Stage, etc.)</param>
    void Apply(Entity user, Entity target, Object context = null, Object source = null);
}

using UnityEngine;

namespace Nestlabs.Obstacle
{
    public interface IHittable
    {
        void OnHit();
    }

    public abstract class ObstacleBase : MonoBehaviour, IHittable
    {
        public virtual void OnHit()
        {
            // generic effect for the obstacle
        }
    }
}

using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        // Damage uses an inset body, independently of terrain collision,
        // pickups and the wider foot area used for stomping. Sprite corners
        // and a glancing overlap should not cost a life.
        Rect PlayerHurtBounds => new Rect(player.X+2,player.Y+2,8,PlayerHeight-4);
        static Rect ContactDamageBounds(Actor a)
        {
            // Normal 16px enemies have 2px padding on each edge. Scale the
            // padding down for small flames instead of erasing their hitbox.
            float dx=Mathf.Min(2,a.w*.125f),dy=Mathf.Min(2,a.h*.125f);
            return new Rect(a.x+dx,a.y+dy,a.w-2*dx,a.h-2*dy);
        }
        bool TouchesDamage(Actor a)=>PlayerHurtBounds.Overlaps(ContactDamageBounds(a));
    }
}

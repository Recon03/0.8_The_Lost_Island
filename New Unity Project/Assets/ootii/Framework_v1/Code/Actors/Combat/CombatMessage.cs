using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Message
    /// </summary>
    public class CombatMessage : DamageMessage
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_UNKNOWN = 1000;
        public static int MSG_COMBATANT_CANCEL = 1001;
        public static int MSG_COMBATANT_ATTACK = 1002;
        public static int MSG_COMBATANT_BLOCK = 1003;
        public static int MSG_COMBATANT_PARRY = 1004;
        public static int MSG_COMBATANT_EVADE = 1005;
        public static int MSG_ATTACKER_ATTACKED = 1101;
        public static int MSG_DEFENDER_IGNORED = 1102;
        public static int MSG_DEFENDER_BLOCKED = 1103;
        public static int MSG_DEFENDER_PARRIED = 1104;
        public static int MSG_DEFENDER_EVADED = 1105;
        public static int MSG_DEFENDER_DAMAGED = 1107;
        public static int MSG_DEFENDER_KILLED = 1108;

        public static string[] Names = new string[]
        {
            "Unknown",
            "Combatant Cancel",
            "Combatant Attack",
            "Combatant Block",
            "Combatant Parry",
            "Combatant Evade",
            "Attacker Attacked",
            "Defender Ignored",
            "Defender Blocked",
            "Defender Parried",
            "Defender Evaded", 
            "Defender Damaged",
            "Defender Killed"
        };

        /// <summary>
        /// Combatant that represents the attacker
        /// </summary>
        public GameObject Attacker = null;

        /// <summary>
        /// Combatant that represents the defender
        /// </summary>
        public GameObject Defender = null;

        /// <summary>
        /// Weapon that did the damage
        /// </summary>
        public IWeaponCore Weapon = null;

        /// <summary>
        /// Index used to direct an action
        /// </summary>
        public int StyleIndex = -1;

        /// <summary>
        /// Combat style actually being used
        /// </summary>
        public ICombatStyle CombatStyle = null;

        /// <summary>
        /// Amount of impulse to add when the attack occurs
        /// </summary>
        public float ImpactPower = 0f;

        /// <summary>
        /// Vector that is the direction of the attack's velocity (in world-space)
        /// </summary>
        public Vector3 HitVector = Vector3.zero;

        /// <summary>
        /// Closest bone that was hit
        /// </summary>
        public Transform HitTransform = null;

        /// <summary>
        /// Point (in world-space) where the impact occured
        /// </summary>
        public Vector3 HitPoint = Vector3.zero;

        /// <summary>
        /// Direction (in local-space) that points to the impact hit point from the defender's combat origin
        /// </summary>
        public Vector3 HitDirection = Vector3.zero;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Attacker = null;
            Defender = null;
            Weapon = null;
            StyleIndex = -1;
            CombatStyle = null;
            HitTransform = null;

            base.Clear();
        }

        /// <summary>
        /// Release this instance.
        /// </summary>
        public override void Release()
        {
            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            IsSent = true;
            IsHandled = true;

            // Make it available to others.
            if (this is CombatMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<CombatMessage> sPool = new ObjectPool<CombatMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static CombatMessage Allocate()
        {
            // Grab the next available object
            CombatMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new CombatMessage(); }

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static CombatMessage Allocate(CombatMessage rSource)
        {
            // Grab the next available object
            CombatMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new CombatMessage(); }

            lInstance.Attacker = rSource.Attacker;
            lInstance.Defender = rSource.Defender;
            lInstance.Weapon = rSource.Weapon;
            lInstance.StyleIndex = rSource.StyleIndex;
            lInstance.CombatStyle = rSource.CombatStyle;
            lInstance.Damage = rSource.Damage;
            lInstance.DamageType = rSource.DamageType;
            lInstance.ImpactPower = rSource.ImpactPower;
            lInstance.HitVector = rSource.HitVector;
            lInstance.HitTransform = rSource.HitTransform;
            lInstance.HitPoint = rSource.HitPoint;
            lInstance.HitDirection = rSource.HitDirection;

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(CombatMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            sPool.Release(rInstance);
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public new static void Release(IMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            if (rInstance is CombatMessage)
            {
                sPool.Release((CombatMessage)rInstance);
            }
        }
    }
}

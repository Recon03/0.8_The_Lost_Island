using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;
using com.ootii.Messages;
using com.ootii.Utilities;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Determines the capabilities of the actor and provides access to
    /// core specific functionality.
    /// </summary>
    public class ActorCore : MonoBehaviour, IActorCore
    {
        /// <summary>
        /// GameObject that owns the IAttributeSource we really want
        /// </summary>
        public GameObject _AttributeSourceOwner = null;
        public GameObject AttributeSourceOwner
        {
            get { return _AttributeSourceOwner; }
            set { _AttributeSourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of the attributes that control our health
        /// </summary>
        [NonSerialized]
        protected IAttributeSource mAttributeSource = null;
        public IAttributeSource AttributeSource
        {
            get { return mAttributeSource; }
            set { mAttributeSource = value; }
        }

        /// <summary>
        /// Transform that is the actor
        /// </summary>
        public Transform Transform
        {
            get { return gameObject.transform; }
        }

        /// <summary>
        /// Determines if the actor is actually alive
        /// </summary>
        public bool _IsAlive = true;
        public virtual bool IsAlive
        {
            get { return _IsAlive; }
            set { _IsAlive = value; }
        }

        /// <summary>
        /// Attribute identifier that represents the health attribute
        /// </summary>
        public string _HealthID = "HEALTH";
        public string HealthID
        {
            get { return _HealthID; }
            set { _HealthID = value; }
        }

        /// <summary>
        /// Motion name to use when damage is taken
        /// </summary>
        public string _DamagedMotion = "Bow_Damaged";
        public string DamagedMotion
        {
            get { return _DamagedMotion; }
            set { _DamagedMotion = value; }
        }

        /// <summary>
        /// Motion name to use when death occurs
        /// </summary>
        public string _DeathMotion = "Bow_Death";
        public string DeathMotion
        {
            get { return _DeathMotion; }
            set { _DeathMotion = value; }
        }

        /// <summary>
        /// Effects that are active on the actor. These can do things like modify heal over time.
        /// </summary>
        public List<ActorCoreEffect> _ActiveEffects = new List<ActorCoreEffect>();
        public List<ActorCoreEffect> ActiveEffects
        {
            get { return _ActiveEffects; }
            set { _ActiveEffects = value; }
        }

        /// <summary>
        /// Serialized effects since Unity can't serialized derived classes
        /// </summary>
        public List<string> _EffectDefinitions = new List<string>();

#if UNITY_EDITOR

        // Keeps the effect selected in the editor
        public int EditorEffectIndex = -1;

#endif

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected virtual void Awake()
        {
            // Object that will provide access to attributes
            if (_AttributeSourceOwner != null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(_AttributeSourceOwner);
            }

            // If the input source is still null, see if we can grab a local input source
            if (AttributeSource == null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(gameObject);
                if (AttributeSource != null) { _AttributeSourceOwner = gameObject; }
            }

            // Create and initialize the effects
            InstantiateEffects();
        }

        /// <summary>
        /// Grabs the active effect whose name matches
        /// </summary>
        /// <param name="rName">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual ActorCoreEffect GetActiveEffectFromName(string rNameID)
        {
            for (int i = 0; i < _ActiveEffects.Count; i++)
            {
                if (_ActiveEffects[i].Name == rNameID)
                {
                    return _ActiveEffects[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Grabs the active effect whose type and name match
        /// </summary>
        /// <typeparam name="T">Type of effect to find</typeparam>
        /// <param name="rSourceID">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual T GetActiveEffectFromName<T>(string rNameID) where T : ActorCoreEffect
        {
            for (int i = 0; i < _ActiveEffects.Count; i++)
            {
                if (_ActiveEffects[i].Name == rNameID)
                {
                    if (_ActiveEffects[i].GetType() == typeof(T))
                    {
                        return (T)_ActiveEffects[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Grabs the active effect whose type and source ID match
        /// </summary>
        /// <typeparam name="T">Type of effect to find</typeparam>
        /// <param name="rSourceID">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual T GetActiveEffectFromSourceID<T>(string rSourceID) where T : ActorCoreEffect
        {
            for (int i = 0; i < _ActiveEffects.Count; i++)
            {
                if (_ActiveEffects[i].SourceID == rSourceID)
                {
                    if (_ActiveEffects[i].GetType() == typeof(T))
                    {
                        return (T)_ActiveEffects[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Run once per frame in order to manage the actor
        /// </summary>
        protected virtual void Update()
        {
            // Process each of the active effects
            for (int i = 0; i < _ActiveEffects.Count; i++)
            {
                ActorCoreEffect lEffect = _ActiveEffects[i];
                bool lIsActive = lEffect.Update();

                // If the effect is no longer active, remove it
                if (!lIsActive)
                {
                    _ActiveEffects.RemoveAt(i);
                    i--;

                    lEffect.Release();
                }
            }
        }

        /// <summary>
        /// Called when the actor is about to be affected by something like a spell, poison, etc.
        /// The sub-class would override this function and interrogate the message as needed.
        /// </summary>
        /// <param name="rMessage">Message describing what's happening</param>
        /// <returns>Returns true if the affect should continue or false if not</returns>
        public virtual bool TestAffected(IMessage rMessage)
        {
            return true;
        }

        /// <summary>
        /// Called when the actor takes damage. This allows the actor to respond.
        /// Damage Type 0 = Physical melee
        /// Damage Type 1 = Physical ranged
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rDamagedMotion">Motion to activate due to damage</param>
        /// <param name="rDeathMotion">Motion to activate due to death</param>
        /// <returns>Determines if the damage was applied</returns>
        public virtual bool OnDamaged(IMessage rMessage)
        {
            if (!IsAlive) { return true; }

            float lRemainingHealth = 0f;
            if (AttributeSource != null)
            {
                if (rMessage is DamageMessage)
                {
                    lRemainingHealth = AttributeSource.GetAttributeValue(HealthID) - ((DamageMessage)rMessage).Damage;
                    AttributeSource.SetAttributeValue(HealthID, lRemainingHealth);
                }
            }

            if (lRemainingHealth <= 0f)
            {
                OnKilled(rMessage);
            }
            else if (rMessage != null)
            {
                bool lPlayAnimation = true;
                if (rMessage is DamageMessage) { lPlayAnimation = ((DamageMessage)rMessage).AnimationEnabled; }

                if (lPlayAnimation)
                {
                    MotionController lMotionController = gameObject.GetComponent<MotionController>();
                    if (lMotionController != null)
                    {
                        // Send the message to the MC to let it activate
                        rMessage.ID = CombatMessage.MSG_DEFENDER_DAMAGED;
                        lMotionController.SendMessage(rMessage);
                    }

                    if (!rMessage.IsHandled && DamagedMotion.Length > 0)
                    {
                        MotionControllerMotion lMotion = lMotionController.GetMotion(DamagedMotion);
                        if (lMotion != null)
                        {
                            lMotionController.ActivateMotion(lMotion);
                        }
                        else
                        {
                            Animator lAnimator = gameObject.GetComponent<Animator>();
                            if (lAnimator != null) { lAnimator.CrossFade(DamagedMotion, 0.25f); }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tells the actor to die and triggers any effects or animations
        /// Damage Type 0 = Physical melee
        /// Damage Type 1 = Physical ranged
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        /// <param name="rDeathMotion">Motion to activate due to death</param>
        public virtual void OnKilled(IMessage rMessage)
        {
            IsAlive = false;

            if (AttributeSource != null && HealthID.Length > 0)
            {
                AttributeSource.SetAttributeValue(HealthID, 0f);
            }

            StartCoroutine(InternalDeath(rMessage));
        }

        /// <summary>
        /// Coroutine to play the death animation and disable the actor after a couple of seconds
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        /// <returns></returns>
        protected virtual IEnumerator InternalDeath(IMessage rMessage)
        {
            ActorController lActorController = gameObject.GetComponent<ActorController>();
            MotionController lMotionController = gameObject.GetComponent<MotionController>();

            // Run the death animation if we can
            if (rMessage != null && lMotionController != null)
            {
                // Send the message to the MC to let it activate
                rMessage.ID = CombatMessage.MSG_DEFENDER_KILLED;
                lMotionController.SendMessage(rMessage);

                if (!rMessage.IsHandled && DeathMotion.Length > 0)
                {
                    MotionControllerMotion lMotion = lMotionController.GetMotion(DeathMotion);
                    if (lMotion != null)
                    {
                        lMotionController.ActivateMotion(lMotion);
                    }
                    else
                    {
                        Animator lAnimator = gameObject.GetComponent<Animator>();
                        if (lAnimator != null) { lAnimator.CrossFade(DeathMotion, 0.25f); }
                    }
                }

                // Trigger the death animation
                yield return new WaitForSeconds(3.0f);

                // Shut down the MC
                lMotionController.enabled = false;
                lMotionController.ActorController.enabled = false;
            }

            // Disable all colliders
            Collider[] lColliders = gameObject.GetComponents<Collider>();
            for (int i = 0; i < lColliders.Length; i++)
            {
                lColliders[i].enabled = false;
            }

            if (lActorController != null) { lActorController.RemoveBodyShapes(); }
        }

        /// <summary>
        /// Processes the effect definitions and updates the effects to match the definitions.
        /// </summary>
        public void InstantiateEffects()
        {
            int lDefCount = _EffectDefinitions.Count;

            // First, remove any extra motors that may exist
            for (int i = ActiveEffects.Count - 1; i >= lDefCount; i--)
            {
                ActiveEffects.RemoveAt(i);
            }

            // We need to match the motor definitions to the motors
            for (int i = 0; i < lDefCount; i++)
            {
                string lDefinition = _EffectDefinitions[i];

                Type lType = JSONSerializer.GetType(lDefinition);
                if (lType == null) { continue; }

                ActorCoreEffect lEffect = null;

                // If don't have a motor matching the type, we need to create one
                if (ActiveEffects.Count <= i || !lType.Equals(ActiveEffects[i].GetType()))
                {
                    lEffect = Activator.CreateInstance(lType) as ActorCoreEffect;
                    lEffect.ActorCore = this;

                    if (ActiveEffects.Count <= i)
                    {
                        ActiveEffects.Add(lEffect);
                    }
                    else
                    {
                        ActiveEffects[i] = lEffect;
                    }
                }
                // Grab the matching motor
                else
                {
                    lEffect = ActiveEffects[i];
                }

                // Fill the motor with data from the definition
                if (lEffect != null)
                {
                    lEffect.Deserialize(lDefinition);
                }
            }

            // Allow each motion to initialize now that his has been deserialized
            for (int i = 0; i < ActiveEffects.Count; i++)
            {
                ActiveEffects[i].Awake();
            }
        }
    }
}

﻿using OpenH2.Core.Extensions;
using OpenH2.Engine.Components;
using OpenH2.Foundation.Extensions;
using OpenH2.Physics.Core;
using OpenH2.Physics.Proxying;
using System.Numerics;
using System.Threading;

namespace OpenH2.Engine.Systems.Movement
{


    public class DynamicMovementController
    {
        private float desiredAirSpeed = 1.1f;
        private float deltaAirSpeed = 0.4f;
        private float desiredGroundSpeed = 2.5f;
        private float deltaGroundSpeed = 1.0f;
        private float jumpSpeed = 4.7f;

        private ContactInfo GroundContact = null;

        public ControllerState state { get; private set; } = ControllerState.Walking;

        // TODO: this is only present to access IPhysicsImplementation, if we can access it/be provided it, that would be better
        public MoverComponent Mover { get; internal set; }

        public DynamicMovementController()
        {
        }

        public void ContactFound(ContactInfo contact)
        {
            if(contact.IsGroundContact)
            {
                Interlocked.CompareExchange(ref GroundContact, contact, null);
            }
        }

        public void Move(IPhysicsProxy physics,
            Vector3 inputVector,
            Vector3 forward,
            Vector3 strafe)
        {
            var groundNormal = ProcessCollision(physics);

            // align our movement vectors with the ground normal (ground normal = 'up')
            Vector3 newForward = VectorExtensions.OrthoNormalize(groundNormal, forward);
            Vector3 newStrafe = VectorExtensions.OrthoNormalize(groundNormal, strafe);

            // Calculate movement direction
            var desiredMovement = Vector3.Normalize((inputVector.X * newForward) + (inputVector.Y * newStrafe));

            HandleMovement(physics, desiredMovement);

            // Jump
            if (inputVector.Z > 0f)
            {
                TryJump(physics, EngineGlobals.Up);   
            }
        }

        public Vector3 ProcessCollision(IPhysicsProxy physics)
        {
            // Early out if no ground contact since last check
            if(this.GroundContact == null)
            {
                this.state = ControllerState.Falling;
                return EngineGlobals.Up;
            }

            // Handle landing from a fall
            if(this.state == ControllerState.Falling)
            {
                this.state = ControllerState.Walking;
            }

            // TODO: handle walking|sprinting -> sliding
            //    - If velocity exceeds some threshold, transition to sliding
            //    - If we're climbing too steep, transition to sliding

            // TODO: handle sliding -> walking
            //    - If we're slding and drop below threshold and not too steep, transition back to walking

            var contactNormal = GroundContact.Normal;

            GroundContact = null;

            return contactNormal;
        }

        public void HandleMovement(IPhysicsProxy physics, Vector3 movement)
        {
            var xyMovement = Vector3.Normalize(new Vector3(movement.X, movement.Y, 0));

            var xyMagSquared = xyMovement.LengthSquared();

            if (float.IsNaN(xyMagSquared) || xyMagSquared <= 0)
            {
                return;
            }
            
            if (this.state == ControllerState.Walking)
            {
                var velocityChange = GetVelocityChange(physics, xyMovement, desiredGroundSpeed, deltaGroundSpeed);

                if(velocityChange.LengthSquared() > 0)
                {
                    physics.AddVelocity(velocityChange);
                }
            }
            else if (this.state == ControllerState.Falling)
            {
                /*
                    goal: allow, at maximum, desiredAirSpeed velocity while falling
                    edge case: when jumping from movement, or sliding off edge, don't limit speed down to desiredAirSpeed
                 */


                Vector3 currentVelocity = physics.GetVelocity();

                var likeness = Vector3.Dot(xyMovement, Vector3.Normalize(currentVelocity));
                
                if(float.IsNaN(likeness))
                {
                    likeness = 0f;
                }

                var currentVelocityInDesiredDirection = currentVelocity * likeness;

                var deltaMagnitude = desiredAirSpeed - currentVelocityInDesiredDirection.Length();

                if (deltaMagnitude != 0)
                {
                    var change = xyMovement * MathExt.Clamp(deltaMagnitude, 0, deltaAirSpeed);

                    if(change.LengthSquared() > 0)
                    {
                        physics.AddVelocity(change);
                    }
                }
            }
            else
            {
                // TODO: handle sprinting and sliding movement
                // TODO: desired feel during sliding is 0 friction in the movement direction
                return;
            }            
        }

        // BUG: can get 'stuck' at the top edge of a ramp
        private Vector3 GetVelocityChange(IPhysicsProxy physics, Vector3 movement, float desiredMagnitude, float deltaMagnitude)
        {
            Vector3 currentVelocity = physics.GetVelocity();

            movement *= desiredMagnitude;
            // TODO: figure out a better limiting method? velocity going up slopes is limited
            Vector3 velocityChange = (movement - currentVelocity);
            velocityChange.X = MathExt.Clamp(velocityChange.X, -deltaMagnitude, deltaMagnitude);
            velocityChange.Y = MathExt.Clamp(velocityChange.Y, -deltaMagnitude, deltaMagnitude);
            velocityChange.Z = 0;

            return velocityChange;
        }

        private void TryJump(IPhysicsProxy physics, Vector3 jumpDirection)
        {
            if(state != ControllerState.Falling)
            {
                var velocity = Vector3.Multiply(jumpSpeed, jumpDirection);
                physics.AddVelocity(velocity);
                state = ControllerState.Falling;
            }
        }

        public enum ControllerState
        {
            Walking,
            Sprinting,
            Sliding,
            Falling
        }
    }
}

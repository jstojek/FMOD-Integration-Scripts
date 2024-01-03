using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using FMODUnity;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public string jumpAudioEvent;
        public string respawnAudioEvent;
        public string ouchAudioEvent;
        public string footstepsAudioEvent;
        public string landingAudioEvent;

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        FMOD.Studio.EventInstance jumpSound;
        FMOD.Studio.EventInstance respawnSound;
        FMOD.Studio.EventInstance ouchSound;
        FMOD.Studio.EventInstance footstepsSound;
        FMOD.Studio.EventInstance landingSound;

        public Bounds Bounds => collider2d.bounds;

        public float footstepInterval = 0.5f;
        private float footstepTimer = 0f;

        public float enemyDetectionDistance = 2.0f;
        public LayerMask enemyLayer;
        public string enemyNearbyAudioEvent; // FMOD event or other sound event for the situation when the player is close to the enemy.
        public float enemySoundInterval = 2.0f; // Interval between playing enemy sound.
        private float lastEnemySoundTime = 0f; // The time of the last enemy sound playback.

        void Awake()
        {
            health = GetComponent<Health>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            jumpSound = FMODUnity.RuntimeManager.CreateInstance(jumpAudioEvent);
            respawnSound = FMODUnity.RuntimeManager.CreateInstance(respawnAudioEvent);
            ouchSound = FMODUnity.RuntimeManager.CreateInstance(ouchAudioEvent);
            footstepsSound = FMODUnity.RuntimeManager.CreateInstance(footstepsAudioEvent);
            landingSound = FMODUnity.RuntimeManager.CreateInstance(landingAudioEvent);
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                {
                    jumpState = JumpState.PrepareToJump;
                    PlayJumpSound();
                }
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();

            if (IsGrounded && Mathf.Abs(move.x) > 0.01f)
            {
                footstepTimer += Time.deltaTime;
                if (footstepTimer >= footstepInterval)
                {
                    PlayFootstepsSound();
                    footstepTimer = 0f;
                }
            }

            // Check if the player is near the enemy.
            CheckForNearbyEnemy();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                        PlayLandingSound();
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        void PlayJumpSound()
        {
            jumpSound.start();
        }

        void PlayRespawnSound()
        {
            respawnSound.start();
        }

        void PlayOuchSound()
        {
            ouchSound.start();
        }

        void PlayFootstepsSound()
        {
            footstepsSound.start();
        }

        void PlayLandingSound()
        {
            landingSound.start();
        }

        void CheckForNearbyEnemy()
        {
            if (Time.time - lastEnemySoundTime >= enemySoundInterval)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, enemyDetectionDistance, enemyLayer);

                if (colliders.Length > 0)
                {
                    Debug.Log("The player is close to the enemy!");
                    PlayEnemyNearbySound();
                    lastEnemySoundTime = Time.time; // Update the time of the last enemy sound playback.
                }
            }
        }

        void PlayEnemyNearbySound()
        {
            if (!string.IsNullOrEmpty(enemyNearbyAudioEvent))
            {
                FMOD.Studio.EventInstance enemyNearbySound = FMODUnity.RuntimeManager.CreateInstance(enemyNearbyAudioEvent);
                enemyNearbySound.start();
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}

DEMO: https://youtu.be/yygOuGd_e48?feature=shared

PlayerController.cs
Modified script to integrate with FMOD.
Adding playing custom events through FMOD to the script.
Checking when the player is on the ground (after jumping) to play a sound.
Creating a step sound playback interval to synchronize with the animation.
Playing enemy sound when the player is nearby and also adjusting the playback frequency.

EnemyController.cs
Modified script to integrate with FMOD.
Adding playing custom events through FMOD to the script.
Playing a sound when the enemy collides with the player.

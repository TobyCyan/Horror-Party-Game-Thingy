using System;
using UnityEngine;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace com.mansion.Entities.Audio
{

    public class FootstepAudioPlayer : MonoBehaviour
    {

        [SerializeField]
        private FootstepAudioSamples _defaultAudioSamples;
        [SerializeField]
        private AudioSource _audioSource;
        [SerializeField]
        private float _speedModifier = 0.1f;

        private float _speed;
        private float _t;

        private PlayerMovement _movement;

        private void Start()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            // Might be playing too fast
            if (!_movement.moveInput.Equals(UnityEngine.Vector2.zero))
            {
                _t += Time.deltaTime * _movement.movementSpeed * _speedModifier;
            }

            if (_t >= 3f)
            {
                _t = _t % 3f;
            
                var audio = _defaultAudioSamples.PickRandom();
                _audioSource.PlayOneShot(audio);
            }
        }
    }
}
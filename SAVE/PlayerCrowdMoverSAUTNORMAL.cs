using System;
using Data;
using Helpers;
using UnityEngine;

namespace Components
{
    public class PlayerCrowdMover : MonoBehaviour
    {
        private Crowd _playerCrowd;
        private Vector3 _leashPos;
        private bool _isJumping;
        private float _jumpHeight = 2f;
        private float _jumpDuration = 0.6f;
        private float _jumpTimer;

        private bool _isSlowed;
        private float _slowTimer;
        private const float SLOW_DURATION = 2f;
        private const float SLOW_MULTIPLIER = 0.5f;

        private float _crowdSpeed;
        private float _manSpeed;
        private float _manRotSpeed;
        private float _roadHalfWidth;
        private float _initialY;

        private bool _isStopped = true;

        public void Initialize(float crowdSpeed, float manSpeed, float manRotSpeed)
        {
            _roadHalfWidth = ValuesCounter.ROAD_WIDTH * 0.5f;
            _crowdSpeed = crowdSpeed;
            _manSpeed = manSpeed;
            _manRotSpeed = manRotSpeed;

            GameEvents.OnDrag += MoveLeashX;
            GameEvents.OnJump += Jump;
            GameEvents.OnSlowDown += SlowDown;
            GameEvents.OnLevelLoad += GetLevelData;
            GameEvents.OnLevelStartClick += RunTheCrowd;
            GameEvents.OnCrowdFightStart += SetCrowdFightMode;
            GameEvents.OnBossFightStart += SetCrowdFightMode;
            GameEvents.OnCrowdFightEnd += RunTheCrowd;
            GameEvents.OnFinishAchieved += StopTheCrowd;
            GameEvents.OnGameLoose += ReInit;
            GameEvents.OnGameUpdate += OnUpdate;
        }

        private void ReInit()
        {
            StopTheCrowd();
            _playerCrowd = null;
            _leashPos = Vector3.zero;
            _isJumping = false;
            _jumpTimer = 0f;
            _isSlowed = false;
            _slowTimer = 0f;
        }

        public void DeInitialize()
        {
            StopTheCrowd();
            GameEvents.OnGameUpdate -= OnUpdate;
            GameEvents.OnDrag -= MoveLeashX;
            GameEvents.OnJump -= Jump;
            GameEvents.OnSlowDown -= SlowDown;
            GameEvents.OnLevelLoad -= GetLevelData;
            GameEvents.OnLevelStartClick -= RunTheCrowd;
            GameEvents.OnCrowdFightStart -= SetCrowdFightMode;
            GameEvents.OnBossFightStart -= SetCrowdFightMode;
            GameEvents.OnCrowdFightEnd -= RunTheCrowd;
            GameEvents.OnGameLoose -= ReInit;
            GameEvents.OnFinishAchieved -= StopTheCrowd;
        }

        private void GetLevelData(Level level)
        {
            _playerCrowd = level.PlayerCrowd;
            _leashPos = _playerCrowd.WorldPos;
            _initialY = _leashPos.y;
        }

        private void MoveLeashX(Vector2 dragDirection)
        {
            var step = ValuesCounter.ROAD_WIDTH * (dragDirection.x / Screen.width);
            _leashPos.x += step;
            _leashPos.x = Math.Clamp(_leashPos.x, -_roadHalfWidth, _roadHalfWidth);
        }

        private void Jump()
        {
            if (!_isJumping && !_isStopped)
            {
                _isJumping = true;
                _jumpTimer = 0f;
            }
        }

        private void SlowDown()
        {
            if (!_isStopped)
            {
                _isSlowed = true;
                _slowTimer = 0f;
            }
        }

        private void UpdateJump(float deltaTime)
        {
            if (_isJumping)
            {
                _jumpTimer += deltaTime;
                float jumpProgress = _jumpTimer / _jumpDuration;
                
                if (jumpProgress >= 1f)
                {
                    _isJumping = false;
                    _leashPos.y = _initialY;
                }
                else
                {
                    float heightMultiplier = Mathf.Sin(jumpProgress * Mathf.PI);
                    _leashPos.y = _initialY + (_jumpHeight * heightMultiplier);
                }
            }
        }

        private void UpdateSlow(float deltaTime)
        {
            if (_isSlowed)
            {
                _slowTimer += deltaTime;
                if (_slowTimer >= SLOW_DURATION)
                {
                    _isSlowed = false;
                }
            }
        }

        private void MoveLeashZ(float deltaSpeed)
        {
            _leashPos.z += deltaSpeed;
        }

        private void MoveMen(float deltaSpeed)
        {
            var men = _playerCrowd.Men;
            foreach (var man in men)
            {
                var manTr = man.transform;
                var manPos = manTr.position;
                var manRot = manTr.rotation;
                var targetPos = _leashPos + man.TargetLocalPos;
                var targetRot = Quaternion.LookRotation((targetPos - manPos).normalized);

                manTr.position = Vector3.Lerp(manPos, targetPos, deltaSpeed);
                manTr.rotation = Quaternion.Lerp(manRot, targetRot, deltaSpeed * _manRotSpeed);
            }
        }

        private void StopTheCrowd()
        {
            _isStopped = true;
        }

        private void RunTheCrowd()
        {
            _isStopped = false;
            _playerCrowd.SetAnimation(AnimationType.Run);
        }

        private void SetCrowdFightMode()
        {
            StopTheCrowd();
            _playerCrowd.SetAnimation(AnimationType.Fight);
            _isStopped = true;
        }

        private void UpdateLabelPositioning(float deltaSpeed)
        {
            var labelTr = _playerCrowd.MenCountLabel.transform;
            var curPos = labelTr.position;
            var targetPos = _leashPos;
            targetPos.y = curPos.y;
            labelTr.position = Vector3.Lerp(curPos, targetPos, deltaSpeed);
        }

        private void OnUpdate(float deltaTime)
{
    if (_isStopped || !_playerCrowd)
    {
        return;
    }

    // Update the slow effect timer.
    UpdateSlow(deltaTime);

    // If slowdown is active, rotate the player.
    if (_isSlowed)
    {
        // Calculate the incremental rotation (360° spread over SLOW_DURATION seconds).
        float deltaRotation = (360f / SLOW_DURATION) * deltaTime;
        transform.Rotate(0f, deltaRotation, 0f);
    }

    // Use the slowdown multiplier to reduce the forward speed.
    float speedMultiplier = _isSlowed ? SLOW_MULTIPLIER : 1f;
    float deltaCrowdSpeed = _crowdSpeed * deltaTime * speedMultiplier;
    float deltaManSpeed = _manSpeed * deltaTime * speedMultiplier;

    // Update vertical movement if jumping.
    UpdateJump(deltaTime);
    // Move forward along the z-axis.
    MoveLeashZ(deltaCrowdSpeed);
    // Move the individual members of the crowd.
    MoveMen(deltaManSpeed);
    // Update the position of the label.
    UpdateLabelPositioning(deltaCrowdSpeed);
}

    }
}
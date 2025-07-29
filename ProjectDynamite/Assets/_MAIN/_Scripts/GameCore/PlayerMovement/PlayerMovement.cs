using UnityEngine;

namespace Galich.GameCore
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStatsSO _movementStats;
        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private float _verticalVelocity;

        private Rigidbody2D _rigidBody;
        private Vector2 _moveVelocity;
        private bool _isFacingRight;
        private RaycastHit2D _groundHit;
        private RaycastHit2D _headHit;
        private bool _isGrounded;
        private bool _bumpedHead;
        private Vector2 _targetVelocity = Vector2.zero;
        private bool _isJumping;
        private bool _isFastFalling;
        private bool _isFalling;
        private float _fastFallTime;
        private float _fastFallReleaseSpeed;
        private int _numberOfJumpsUsed;
        private float _apexPoint;
        private float _timePastApexThreshold;
        private bool _isPastApexThreshold;
        private float _jumpBufferTime;
        private bool _jumpReleaseDuringBuffer;
        private float _coyoteTimer;

        private void Awake()
        {
            _isFacingRight = true;
            _rigidBody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            CountTimers();
            JumpChecks();
        }

        private void FixedUpdate()
        {
            CollisionChecks();
            Jump();

            if (_isGrounded)
            {
                Move(_movementStats.GroundAcceleration, _movementStats.GroundDeceleration, InputManager.Movement);
            }
            else
            {
                Move(_movementStats.AirAcceleration, _movementStats.AirDeceleration, InputManager.Movement);
            }
        }

        #region Timers

        private void CountTimers()
        {
            _jumpBufferTime -= Time.deltaTime;

            if (!_isGrounded)
            {
                _coyoteTimer -= Time.deltaTime;
            }
            else
            {
                _coyoteTimer = _movementStats.JumpCoyoteTime;
            }
        }

        #endregion

        #region Jump

        private void JumpButtonPressed()
        {
            if (InputManager.JumpWasPressed)
            {
                _jumpBufferTime = _movementStats.JumpBufferTime;
                _jumpReleaseDuringBuffer = false;
            }
        }

        private void JumpButtonReleased()
        {
            if (InputManager.JumpWasReleased)
            {
                if (_jumpBufferTime > 0f)
                {
                    _jumpReleaseDuringBuffer = true;
                }

                if (_isJumping && _verticalVelocity > 0f)
                {
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                        _isFastFalling = true;
                        _fastFallTime = _movementStats.TimeForUpwardsCancel;
                        _verticalVelocity = 0f;
                    }
                    else
                    {
                        _isFastFalling = true;
                        _fastFallReleaseSpeed = _verticalVelocity;
                    }
                }
            }
        }

        private void InitiateJump()
        {
            // jump with buffer and coyote
            if (_jumpBufferTime > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
            {
                InitiateJump(1);

                if (_jumpReleaseDuringBuffer)
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = _verticalVelocity;
                }
            }

            // double jump
            else if (_jumpBufferTime > 0f && _isJumping && _numberOfJumpsUsed < _movementStats.NumberOfJumpsAllowed)
            {
                _isFastFalling = false;
                InitiateJump(1);
            }

            // air jump after coyote time lapsed
            else if (_jumpBufferTime > 0f && _isFalling && _numberOfJumpsUsed < _movementStats.NumberOfJumpsAllowed - 1)
            {
                InitiateJump(2);
                _isFalling = false;
            }
        }

        private void Landed()
        {
            if ((_isJumping || _isFalling) && _isGrounded && _verticalVelocity <= 0f)
            {
                _isJumping = false;
                _isFalling = false;
                _isFastFalling = false;
                _fastFallTime = 0f;
                _isPastApexThreshold = false;
                _numberOfJumpsUsed = 0;
                _verticalVelocity = Physics2D.gravity.y;
            }
        }

        private void JumpChecks()
        {
            JumpButtonPressed();
            JumpButtonReleased();
            InitiateJump();
            Landed();
        }

        private void InitiateJump(int numberOfJumpsUsed)
        {
            if (!_isJumping)
            {
                _isJumping = true;
            }

            _jumpBufferTime = 0f;
            _numberOfJumpsUsed += numberOfJumpsUsed;
            _verticalVelocity = _movementStats.InitialJumpVelocity;
        }

        private void CheckForHeadBump()
        {
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }
        }

        private void ApplyGravityWhileJumping()
        {
            // apply gravity while jumping
            if (_isJumping)
            {
                CheckForHeadBump();

                // gravity on ascending
                if (_verticalVelocity >= 0f)
                {
                    // apex controls
                    _apexPoint = Mathf.InverseLerp(_movementStats.InitialJumpVelocity, 0f, _verticalVelocity);

                    if (_apexPoint > _movementStats.ApexThreshold)
                    {
                        if (!_isPastApexThreshold)
                        {
                            _isPastApexThreshold = true;
                            _timePastApexThreshold = 0f;
                        }
                        if (_isPastApexThreshold)
                        {
                            _timePastApexThreshold += Time.fixedDeltaTime;

                            if (_timePastApexThreshold < _movementStats.ApexHangTime)
                            {
                                _verticalVelocity = 0f;
                            }
                            else
                            {
                                _verticalVelocity = -0.01f;
                            }
                        }
                    }

                    // gravity on ascending but not past apex threshold
                    else
                    {
                        _verticalVelocity += _movementStats.Gravity * Time.fixedDeltaTime;

                        if (_isPastApexThreshold)
                        {
                            _isPastApexThreshold = false;
                        }
                    }
                }

                // gravity on descending
                else if (!_isFastFalling)
                {
                    _verticalVelocity += _movementStats.Gravity * _movementStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (_verticalVelocity < 0f)
                {
                    if (!_isFalling)
                    {
                        _isFalling = true;
                    }
                }
            }
        }

        private void JumpCut()
        {
            if (_isFastFalling)
            {
                if (_fastFallTime >= _movementStats.TimeForUpwardsCancel)
                {
                    _verticalVelocity += _movementStats.Gravity * _movementStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (_fastFallTime < _movementStats.TimeForUpwardsCancel)
                {
                    _verticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, (_fastFallTime / _movementStats.TimeForUpwardsCancel));
                }

                _fastFallTime += Time.fixedDeltaTime;
            }
        }

        private void NormalGravityWhileFalling()
        {
            if (!_isGrounded && !_isJumping)
            {
                if (!_isFalling)
                {
                    _isFalling = true;
                }

                _verticalVelocity += _movementStats.Gravity * Time.fixedDeltaTime;
            }
        }

        private void Jump()
        {
            ApplyGravityWhileJumping();

            JumpCut();

            NormalGravityWhileFalling();

            _verticalVelocity = Mathf.Clamp(_verticalVelocity, -_movementStats.MaxFallSpeed, 50f);
            _rigidBody.linearVelocity = new(_rigidBody.linearVelocity.x, _verticalVelocity);
        }

        #endregion

        #region Movement

        private void Move(float acceleration, float deceleration, Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
            {
                TurnCheck(moveInput);

                if (InputManager.RunIsHeld)
                {
                    _targetVelocity = new Vector2(moveInput.x, 0f) * _movementStats.MaxRunSpeed;
                }
                else
                {
                    _targetVelocity = new Vector2(moveInput.x, 0f) * _movementStats.MaxWalkSpeed;
                }

                _moveVelocity = Vector2.Lerp(_moveVelocity, _targetVelocity, acceleration * Time.fixedDeltaTime);
                _rigidBody.linearVelocity = new(_moveVelocity.x, _rigidBody.linearVelocity.y);
            }
            else if (moveInput == Vector2.zero)
            {
                _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                _rigidBody.linearVelocity = new(_moveVelocity.x, _rigidBody.linearVelocity.y);
            }
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (_isFacingRight && moveInput.x < 0)
            {
                Turn(false);
            }
            else if (!_isFacingRight && moveInput.x > 0)
            {
                Turn(true);
            }
        }

        private void Turn(bool turnRight)
        {
            if (turnRight)
            {
                _isFacingRight = true;
                transform.Rotate(0f, 180f, 0f);
            }
            else
            {
                _isFacingRight = false;
                transform.Rotate(0f, -180f, 0f);
            }
        }

        #endregion

        #region Collision Checks

        private void BumpedHead()
        {
            Vector2 boxCastOrigin = new(_feetCollider.bounds.center.x, _bodyCollider.bounds.max.y);
            Vector2 boxCastSize = new(_feetCollider.bounds.size.x * _movementStats.HeadWidth, _movementStats.HeadDetectionRayLength);

            _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, _movementStats.HeadDetectionRayLength, _movementStats.GroundLayer);

            if (_headHit.collider != null)
            {
                _bumpedHead = true;
            }
            else
            {
                _bumpedHead = false;
            }

            if (_movementStats.DebugShowHeadBumpBox)
            {
                Color rayColor;

                if (_bumpedHead)
                {
                    rayColor = Color.green;
                }
                else
                {
                    rayColor = Color.red;
                }

                Debug.DrawRay(new(boxCastOrigin.x - boxCastSize.x / 2 * _movementStats.HeadWidth, boxCastOrigin.y), Vector2.up * _movementStats.HeadDetectionRayLength, rayColor);
                Debug.DrawRay(new(boxCastOrigin.x + (boxCastSize.x / 2) * _movementStats.HeadWidth, boxCastOrigin.y), Vector2.up * _movementStats.HeadDetectionRayLength, rayColor);
                Debug.DrawRay(new(boxCastOrigin.x - boxCastSize.x / 2 * _movementStats.HeadWidth, boxCastOrigin.y + _movementStats.HeadDetectionRayLength), _movementStats.HeadWidth * boxCastSize.x * Vector2.right, rayColor);
            }
        }

        private void IsGrounded()
        {
            Vector2 boxCastOrigin = new(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            Vector2 boxCastSize = new(_feetCollider.bounds.size.x, _movementStats.GroundDetectionRayLength);

            _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, _movementStats.GroundDetectionRayLength, _movementStats.GroundLayer);

            if (_groundHit.collider != null)
            {
                _isGrounded = true;
            }
            else
            {
                _isGrounded = false;
            }

            if (_movementStats.DebugShowIsGroundedBox)
            {
                Color rayColor;

                if (_isGrounded)
                {
                    rayColor = Color.green;
                }
                else
                {
                    rayColor = Color.red;
                }

                Debug.DrawRay(new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * _movementStats.GroundDetectionRayLength, rayColor);
                Debug.DrawRay(new(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * _movementStats.GroundDetectionRayLength, rayColor);
                Debug.DrawRay(new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - _movementStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
            }
        }


        private void CollisionChecks()
        {
            IsGrounded();
            BumpedHead();
        }

        #endregion
    }
}
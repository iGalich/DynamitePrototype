using UnityEngine;

namespace Galich.GameCore
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStatsSO _movementStats;
        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;

        private Rigidbody2D _rigidBody;

        // movement variables
        private float _horizontalVelocity;
        private bool _isFacingRight;
        private float _targetVelocity = 0f;

        // collision check variables
        private RaycastHit2D _groundHit;
        private RaycastHit2D _headHit;
        private RaycastHit2D _wallHit;
        private RaycastHit2D _lastWallHit;
        private bool _isGrounded;
        private bool _bumpedHead;
        private bool _isTouchingWall;

        // jump variables
        [SerializeField] private float _verticalVelocity;
        private bool _isJumping;
        private bool _isFastFalling;
        private bool _isFalling;
        private float _fastFallTime;
        private float _fastFallReleaseSpeed;
        private int _numberOfJumpsUsed;

        // apex variables
        private float _apexPoint;
        private float _timePastApexThreshold;
        private bool _isPastApexThreshold;

        // jump buffer variables
        private float _jumpBufferTime;
        private bool _jumpReleaseDuringBuffer;

        // coyote variables
        private float _coyoteTimer;

        // wall slide variables
        private bool _isWallSliding;
        private bool _isWallSlideFalling;

        // wall jump variables
        private bool _useWallJumpMoveStats;
        private bool _isWallJumping;
        private float _wallJumpTime;
        private bool _isWallJumpFastFalling;
        private bool _isWallJumpFalling;
        private float _wallJumpFastFallTime;
        private float _wallJumpFastFallReleaseSpeed;
        private float _wallJumpPostBufferTimer;
        private float _wallJumpApexPoint;
        private float _timePastWallJumpApexThreshold;
        private bool _isPastWallJumpApexThreshold;

        // dash variables
        private bool _isDashing;
        private bool _isAirDashing;
        private float _dashTimer;
        private float _dashOnGroundTimer;
        private int _numberOfDashesUsed;
        private Vector2 _dashDirection;
        private bool _isDashFastFalling;
        private float _dashFastFallTime;
        private float _dashFastFallReleaseSpeed;

        private void Awake()
        {
            _isFacingRight = true;
            _rigidBody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            CountTimers();
            JumpChecks();
            LandCheck();
            WallSlideCheck();
            WallJumpCheck();
            DashCheck();
        }

        private void FixedUpdate()
        {
            CollisionChecks();
            Jump();
            Fall();
            WallSlide();
            WallJump();
            Dash();

            if (_isGrounded)
            {
                Move(_movementStats.GroundAcceleration, _movementStats.GroundDeceleration, InputManager.Movement);
            }
            else
            {
                // wall jumping
                if (_useWallJumpMoveStats)
                {
                    Move(_movementStats.WallJumpMoveAceeleration, _movementStats.WallJumpMoveDeceleration, InputManager.Movement);
                }
                // airborne
                else
                {
                    Move(_movementStats.AirAcceleration, _movementStats.AirDeceleration, InputManager.Movement);
                }
            }

            ApplyVelocity();
        }

        private void ApplyVelocity()
        {
            if (!_isDashing)
            {
                _verticalVelocity = Mathf.Clamp(_verticalVelocity, -_movementStats.MaxFallSpeed, 50f);
            }
            else
            {
                _verticalVelocity = Mathf.Clamp(_verticalVelocity, -50f, 50f);
            }
            
            _rigidBody.linearVelocity = new(_horizontalVelocity, _verticalVelocity);
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

            if (!ShouldApplyPostWallJumpBuffer())
            {
                _wallJumpPostBufferTimer -= Time.deltaTime;
            }

            // dash timer
            if (_isGrounded)
            {
                _dashOnGroundTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Land/Fall

        private void LandCheck()
        {
            if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || _isWallSliding || _isDashFastFalling) && _isGrounded && _verticalVelocity <= 0f)
            {
                ResetJumpValues();
                StopWallSlide();
                ResetWallJumpValues();
                ResetDashes();
                _numberOfJumpsUsed = 0;
                _verticalVelocity = Physics2D.gravity.y;

                if (_isDashFastFalling && _isGrounded)
                {
                    ResetDashValues();
                    return;
                }

                ResetDashValues();
            }
        }

        private void Fall()
        {
            if (!_isGrounded && !_isJumping && !_isWallSliding && !_isWallJumping && !_isDashing && !_isDashFastFalling)
            {
                if (!_isFalling)
                {
                    _isFalling = true;
                }

                _verticalVelocity += _movementStats.Gravity * Time.fixedDeltaTime;
            }
        }

        #endregion

        #region Jump

        private void ResetJumpValues()
        {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexThreshold = false;
        }

        private void JumpButtonPressed()
        {
            if (InputManager.JumpWasPressed)
            {
                if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0f) return;
                else if (_isWallSliding || (_isTouchingWall && !_isGrounded)) return;

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
            else if (_jumpBufferTime > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling || _isAirDashing || _isDashFastFalling) && !_isTouchingWall && _numberOfJumpsUsed < _movementStats.NumberOfJumpsAllowed)
            {
                _isFastFalling = false;
                InitiateJump(1);

                if (_isDashFastFalling)
                {
                    _isDashFastFalling = false;
                }
            }

            // air jump after coyote time lapsed
            else if (_jumpBufferTime > 0f && _isFalling && !_isWallSlideFalling && _numberOfJumpsUsed < _movementStats.NumberOfJumpsAllowed - 1)
            {
                InitiateJump(2);
                _isFalling = false;
            }
        }

        private void JumpChecks()
        {
            JumpButtonPressed();
            JumpButtonReleased();
            InitiateJump();
        }

        private void InitiateJump(int numberOfJumpsUsed)
        {
            if (!_isJumping)
            {
                _isJumping = true;
            }

            ResetWallJumpValues();
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
                    else if (!_isFastFalling)
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

        private void Jump()
        {
            ApplyGravityWhileJumping();

            JumpCut();
        }

        #endregion

        #region Movement

        private void Move(float acceleration, float deceleration, Vector2 moveInput)
        {
            if (_isDashing) return;

            if (Mathf.Abs(moveInput.x) >= _movementStats.MoveThreshold)
            {
                TurnCheck(moveInput);

                if (InputManager.RunIsHeld)
                {
                    _targetVelocity = moveInput.x * _movementStats.MaxRunSpeed;
                }
                else
                {
                    _targetVelocity = moveInput.x * _movementStats.MaxWalkSpeed;
                }

                _horizontalVelocity = Mathf.Lerp(_horizontalVelocity, _targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            else if (Mathf.Abs(moveInput.x) < _movementStats.MoveThreshold)
            {
                _horizontalVelocity = Mathf.Lerp(_horizontalVelocity, 0f, deceleration * Time.fixedDeltaTime);
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

        #region Wall Slide

        private void WallSlideCheck()
        {
            if (_isTouchingWall && !_isGrounded && !_isDashing)
            {
                if (_verticalVelocity < 0f && !_isWallSliding)
                {
                    ResetJumpValues();
                    ResetWallJumpValues();
                    ResetDashValues();

                    if (_movementStats.ResetDashOnWallSlide)
                    {
                        ResetDashes();
                    }

                    _isWallSlideFalling = false;
                    _isWallSliding = true;

                    if (_movementStats.ResetJumpsOnWallSlide)
                    {
                        _numberOfJumpsUsed = 0;
                    }
                }
            }
            else if (_isWallSliding && !_isTouchingWall && !_isGrounded && !_isWallSlideFalling)
            {
                _isWallSlideFalling = true;
                StopWallSlide();
            }
            else
            {
                StopWallSlide();
            }
        }

        private void StopWallSlide()
        {
            if (_isWallSliding)
            {
                _numberOfJumpsUsed++;
                _isWallSliding = false;
            }
        }

        private void WallSlide()
        {
            if (_isWallSliding)
            {
                _verticalVelocity = Mathf.Lerp(_verticalVelocity, -_movementStats.WallSlideSpeed, _movementStats.WallSlideDecelrationSpeed * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Wall Jump

        private void WallJumpFastFalling()
        {
            if (InputManager.JumpWasReleased && !_isWallSliding && !_isTouchingWall && _isWallJumping)
            {
                if (_verticalVelocity > 0f)
                {
                    if (_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = false;
                        _isWallJumpFastFalling = true;
                        _wallJumpFastFallTime = _movementStats.TimeForUpwardsCancel;
                        _verticalVelocity = 0f;
                    }
                    else
                    {
                        _isWallJumpFastFalling = true;
                        _wallJumpFastFallReleaseSpeed = _verticalVelocity;
                    }
                }
            }
        }

        private void WallJumpCheck()
        {
            if (ShouldApplyPostWallJumpBuffer())
            {
                _wallJumpPostBufferTimer = _movementStats.WallJumpPostBufferTime;
            }

            WallJumpFastFalling();

            // actual jump with post wall jump buffer time
            if (InputManager.JumpWasPressed && _wallJumpPostBufferTimer > 0f)
            {
                InitiateWallJump();
            }
        }

        private int _directionMultiplier = 0;
        private Vector2 _hitPoint;

        private void InitiateWallJump()
        {
            if (!_isWallJumping)
            {
                _isWallJumping = true;
                _useWallJumpMoveStats = true;
            }

            StopWallSlide();
            ResetJumpValues();
            _wallJumpTime = 0f;
            _verticalVelocity = _movementStats.InitialWallJumpVelocity;
            _directionMultiplier = 0;
            _hitPoint = _lastWallHit.collider.ClosestPoint(_bodyCollider.bounds.center);

            if (_hitPoint.x > transform.position.x)
            {
                _directionMultiplier = - 1;
            }
            else
            {
                _directionMultiplier = 1;
            }

            _horizontalVelocity = Mathf.Abs(_movementStats.WallJumpDirection.x) * _directionMultiplier;
        }

        private void WallJump()
        {
            // apply wall jump gravity
            if (_isWallJumping)
            {
                // time to take over movement controls while wall jumping
                _wallJumpTime += Time.fixedDeltaTime;
                
                if (_wallJumpTime >= _movementStats.TimeTillJumpApex)
                {
                    _useWallJumpMoveStats = false;
                }

                // hit head
                if (_bumpedHead)
                {
                    _isWallJumpFastFalling = true;
                    _useWallJumpMoveStats = false;
                }

                // gravity in ascending
                if (_verticalVelocity >= 0f)
                {
                    // apex controls
                    _wallJumpApexPoint = Mathf.InverseLerp(_movementStats.WallJumpDirection.y, 0f, _verticalVelocity);

                    if (_wallJumpApexPoint > _movementStats.ApexThreshold)
                    {
                        if (!_isPastWallJumpApexThreshold)
                        {
                            _isPastWallJumpApexThreshold = true;
                            _timePastWallJumpApexThreshold = 0f;
                        }

                        if (_isPastWallJumpApexThreshold)
                        {
                            _timePastWallJumpApexThreshold += Time.fixedDeltaTime;

                            if (_timePastWallJumpApexThreshold < _movementStats.ApexHangTime)
                            {
                                _verticalVelocity = 0f;
                            }
                            else
                            {
                                _verticalVelocity = -0.01f;
                            }
                        }
                    }

                    // gravity in ascending but not past apex threshold
                    else if (!_isWallJumpFastFalling)
                    {
                        _verticalVelocity += _movementStats.WallJumpGravity * Time.fixedDeltaTime;

                        if (_isPastWallJumpApexThreshold)
                        {
                            _isPastWallJumpApexThreshold = false;
                        }
                    }
                }

                // gravity on ascending
                else if (!_isWallJumpFastFalling)
                {
                    _verticalVelocity += _movementStats.WallJumpGravity * Time.fixedDeltaTime;
                }
                else if (_verticalVelocity < 0f)
                {
                    if (!_isWallJumpFalling)
                        _isWallJumpFalling = true;
                }
            }

            // handle wall jump cut
            if (_isWallJumpFastFalling)
            {
                if (_wallJumpFastFallTime >= _movementStats.TimeForUpwardsCancel)
                {
                    _verticalVelocity += _movementStats.WallJumpGravity * _movementStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                else if (_wallJumpFastFallTime < _movementStats.TimeForUpwardsCancel)
                {
                    _verticalVelocity = Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, (_wallJumpFastFallTime / _movementStats.TimeForUpwardsCancel));
                }

                _wallJumpFastFallTime += Time.fixedDeltaTime;
            }
        }

        private bool ShouldApplyPostWallJumpBuffer() => !_isGrounded && (_isTouchingWall || _isWallSliding);

        private void ResetWallJumpValues()
        {
            _isWallSlideFalling = false;
            _useWallJumpMoveStats = false;
            _isWallJumping = false;
            _isWallJumpFastFalling = false;
            _isWallJumpFalling = false;
            _isPastWallJumpApexThreshold = false;
            _wallJumpFastFallTime = 0f;
            _wallJumpTime = 0f;
        }

        #endregion

        #region Dash

        private void DashCheck()
        {
            if (InputManager.DashWasPressed)
            {
                // ground dash
                if (_isGrounded && _dashOnGroundTimer < 0f && !_isDashing)
                {
                    InitiateDash();
                }

                // air dash
                else if (!_isGrounded && !_isDashing && _numberOfDashesUsed < _movementStats.NumberOfDashes)
                {
                    _isAirDashing = true;
                    InitiateDash();

                    // left a wall slide but dashed within the wall jump post buffer time
                    if (_wallJumpPostBufferTimer > 0f)
                    {
                        _numberOfJumpsUsed--;

                        if (_numberOfJumpsUsed < 0)
                        {
                            _numberOfJumpsUsed = 0;
                        }
                    }
                }
            }
        }

        private Vector2 _closestDirection = Vector2.zero;
        private float _minDistance = 0f;
        private float _distance = 0f;
        private bool _isDiagonal;

        private void InitiateDash()
        {
            _dashDirection = InputManager.Movement;
            _closestDirection = Vector2.zero;
            _minDistance = Vector2.Distance(_dashDirection, _movementStats.DashDirections[0]);

            for (int i = 0; i < _movementStats.DashDirections.Length; i++)
            {
                if (_dashDirection == _movementStats.DashDirections[i])
                {
                    _closestDirection = _dashDirection;
                    break;
                }

                _distance = Vector2.Distance(_dashDirection, _movementStats.DashDirections[i]);

                // check if this is a diagonal direction and apply bias
                _isDiagonal = Mathf.Abs(_movementStats.DashDirections[i].x) == 1 && Mathf.Abs(_movementStats.DashDirections[i].y) == 1;
                
                if (_isDiagonal)
                {
                    _distance -= _movementStats.DashDiagonallyBias;
                }
                else if (_distance < _minDistance)
                {
                    _minDistance = _distance;
                    _closestDirection = _movementStats.DashDirections[i];
                }
            }

            // handle direction with no input
            if (_closestDirection == Vector2.zero)
            {
                if (_isFacingRight)
                {
                    _closestDirection = Vector2.right;
                }
                else
                {
                    _closestDirection = Vector2.left;
                }
            }

            _dashDirection = _closestDirection;
            _numberOfDashesUsed++;
            _isDashing = true;
            _dashTimer = 0f;
            _dashOnGroundTimer = _movementStats.TimeBetweenDashesOnGround;

            ResetJumpValues();
            ResetWallJumpValues();
            StopWallSlide();
        }

        private void Dash()
        {
            if (_isDashing)
            {
                // stop the dash after the timer
                _dashTimer += Time.fixedDeltaTime;

                if (_dashTimer >= _movementStats.DashTime)
                {
                    if (_isGrounded)
                    {
                        ResetDashes();
                    }

                    _isAirDashing = false;
                    _isDashing = false;

                    if (!_isJumping && !_isWallJumping)
                    {
                        _dashFastFallTime = 0f;
                        _dashFastFallReleaseSpeed = _verticalVelocity;

                        if (!_isGrounded)
                        {
                            _isDashFastFalling = true;
                        }
                    }

                    return;
                }

                _horizontalVelocity = _movementStats.DashSpeed * _dashDirection.x;

                if (_dashDirection.y != 0f || _isAirDashing)
                {
                    _verticalVelocity = _movementStats.DashSpeed * _dashDirection.y;
                }
            }

            // handle dash cut time
            else if (_isDashFastFalling)
            {
                if (_verticalVelocity > 0f)
                {
                    if (_dashFastFallTime < _movementStats.DashTimeForUpwardsCancel)
                    {
                        _verticalVelocity = Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, _dashFastFallTime / _movementStats.DashTimeForUpwardsCancel);
                    }
                    else if (_dashFastFallTime >= _movementStats.DashTimeForUpwardsCancel)
                    {
                        _verticalVelocity += _movementStats.Gravity * _movementStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                    }

                    _dashFastFallTime += Time.fixedDeltaTime;
                }
                else
                {
                    _verticalVelocity += _movementStats.Gravity * _movementStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
            }
        }

        private void ResetDashValues()
        {
            _isDashFastFalling = false;
            _dashOnGroundTimer = -0.01f;
        }

        private void ResetDashes()
        {
            _numberOfDashesUsed = 0;
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

        private void IsTouchingWall()
        {
            float originEndPoint = 0f;

            if (_isFacingRight)
            {
                originEndPoint = _bodyCollider.bounds.max.x;
            }
            else
            {
                originEndPoint = _bodyCollider.bounds.min.x;
            }

            float adjustedHeight = _bodyCollider.bounds.size.y * _movementStats.WallDetectionRayHeightMultiplier;
            Vector2 boxCastOrigin = new(originEndPoint, _bodyCollider.bounds.center.y);
            Vector2 boxCastSize = new(_movementStats.WallDetectionRayLength, adjustedHeight);

            _wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, _movementStats.WallDetectionRayLength, _movementStats.GroundLayer);

            if (_wallHit.collider != null)
            {
                _lastWallHit = _wallHit;
                _isTouchingWall = true;
            }
            else
            {
                _isTouchingWall = false;
            }

            if (_movementStats.DebugShowWallHitBox)
            {
                Color rayColor;

                if (_isTouchingWall)
                {
                    rayColor = Color.green;
                }
                else
                {
                    rayColor = Color.red;
                }

                Vector2 boxBottomLeft = new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y /2);
                Vector2 boxBottomRight = new(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
                Vector2 boxTopLeft = new(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);
                Vector2 boxTopRight = new(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);

                Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColor);
                Debug.DrawLine(boxBottomRight, boxTopRight, rayColor);
                Debug.DrawLine(boxTopRight, boxTopLeft, rayColor);
                Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColor);
            }
        }

        private void CollisionChecks()
        {
            IsGrounded();
            BumpedHead();
            IsTouchingWall();
        }

        #endregion
    }
}
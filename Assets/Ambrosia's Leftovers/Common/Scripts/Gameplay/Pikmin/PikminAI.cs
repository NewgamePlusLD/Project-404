/*
 * PikminAI.cs
 * Created by: Ambrosia, Kman
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using UnityEngine;

public enum PikminStates {
  Idle,
  RunningTowards,
  Attacking,

  // Holding/Throwing States
  BeingHeld,
  Thrown,

  Dead,
  Waiting,
}

// Immediate states after running towards another object/position
public enum PikminIntention {
  Attack, // TODO
  Carry, // TODO
  PullWeeds, // TODO
  Idle, // TODO (disbanding)
}

public class PikminAI : MonoBehaviour, IHealth {
  [Header ("Components")]
  // Holds everything that makes a Pikmin unique
  public PikminObject _Data = null;
  [SerializeField] LayerMask _PikminInteractableMask = 0;

  [Header ("VFX")]
  [SerializeField] GameObject _DeathParticle = null;
  public Transform _FormationPosition = null;

  #region Debugging Variables

  [Header ("Debugging")]
  [SerializeField] PikminStates _CurrentState = PikminStates.Idle;

  [Header ("Idle")]
  [SerializeField] PikminIntention _Intention = PikminIntention.Idle;
  [SerializeField] Transform _TargetObject = null;
  [SerializeField] Collider _TargetObjectCollider = null;

  [Header ("Attacking")]
  [SerializeField] IPikminAttack _Attacking = null;
  [SerializeField] Vector3 _LatchOffset = Vector3.zero;
  [SerializeField] Transform _AttackingTransform = null;
  [SerializeField] float _AttackTimer = 0;
  [SerializeField] float _AttackJumpTimer = 0;

  [Header ("Stats")]
  [SerializeField] PikminMaturity _CurrentMaturity = PikminMaturity.Leaf;
  [SerializeField] PikminStatSpecifier _CurrentStatSpecifier = default;
  [SerializeField] float _CurrentMoveSpeed = 0;

  [Header ("Misc")]
  [SerializeField] LayerMask _PikminMask = 0;
  [SerializeField] bool _InSquad = false;
  [SerializeField] float _RagdollTime = 0;
  [SerializeField] Vector3 _MovementVector = Vector3.zero;
  [SerializeField] float _PlayerPushScale = 30;
  [SerializeField] float _PikminPushScale = 15;
  #endregion

  // Components
  AudioSource _AudioSource = null;
  Rigidbody _Rigidbody = null;
  CapsuleCollider _Collider = null;

  #region Interface Methods

  float IHealth.GetCurrentHealth () => 1;
  float IHealth.GetMaxHealth () => 1;

  // Empty implementation purposely
  void IHealth.SetHealth (float h) { }

  float IHealth.SubtractHealth (float h) {
    //Pikmin don't have health so they die no matter what
    Die ();
    return 0;
  }

  float IHealth.AddHealth (float h) => 1;

  #endregion

  #region Unity Methods
  void OnEnable () {
    GameObject fObject = new GameObject ($"{name}_formation_pos");
    _FormationPosition = fObject.transform;
  }

  void Awake () {
    _Rigidbody = GetComponent<Rigidbody> ();
    _AudioSource = GetComponent<AudioSource> ();
    _Collider = GetComponent<CapsuleCollider> ();

    _PikminMask = LayerMask.NameToLayer ("Pikmin");

    _CurrentStatSpecifier = PikminStatSpecifier.OnField;
    PikminStatsManager.Add (_Data._Colour, _CurrentMaturity, _CurrentStatSpecifier);
  }

  void Start () {
    _FormationPosition.SetParent (GameManager._Player._FormationCenter.transform);
  }

  void Update () {
    if (GameManager._IsPaused) {
      return;
    }

    switch (_CurrentState) {
      case PikminStates.Idle:
        HandleIdle ();
        break;
      case PikminStates.Attacking:
        HandleAttacking ();
        break;
      case PikminStates.Dead:
        HandleDeath ();
        break;

      case PikminStates.BeingHeld:
      default:
        break;
    }
  }

  void FixedUpdate () {
    if (_CurrentState == PikminStates.RunningTowards)
    {
      if (!_InSquad && _TargetObject == null)
      {
        ChangeState(PikminStates.Idle);
      }
      else if (_InSquad)
      {
        MoveTowards(_FormationPosition.position);
      }
      else
      {
        MoveTowardsTarget();
      }

      if (_Intention == PikminIntention.Attack && _TargetObject != null)
      {
        Vector3 directionToObj = ClosestPointOnTarget() - transform.position;
        if (Physics.Raycast(transform.position, directionToObj, out RaycastHit hit, 2.5f))
        {
          if (hit.collider != _TargetObjectCollider && hit.collider.CompareTag("Pikmin"))
          {
            // Make the Pikmin move to the right a little to avoid jumping into the other Pikmin
            _MovementVector += transform.right * 2.5f;
          }
        }

        _AttackJumpTimer -= Time.deltaTime;
        if (_AttackJumpTimer <= 0 &&
          MathUtil.DistanceTo(transform.position, ClosestPointOnTarget()) <= _Data._AttackDistToJump &&
          Physics.Raycast(transform.position, directionToObj, out hit, 2.5f) &&
          hit.collider == _TargetObjectCollider)
        {
          _MovementVector = new Vector3(_Rigidbody.velocity.x, _Data._AttackJumpPower, _Rigidbody.velocity.z);
          _AttackJumpTimer = _Data._AttackJumpTimer;
        }
      }
    }

    if(MathUtil.DistanceTo(transform.position, GameManager._Player.transform.position, true) < Mathf.Pow(1.5f, 2))
    {
      _MovementVector += (transform.position - GameManager._Player.transform.position).normalized * Time.deltaTime * _PlayerPushScale;
    }

    Collider[] objects = Physics.OverlapSphere(transform.position, 0.5f);
    foreach (Collider collider in objects)
    {
      if (collider.CompareTag("Pikmin"))
      {
        Vector3 direction = transform.position - collider.transform.position;
        direction.y = 0;

        _MovementVector += direction.normalized * Time.deltaTime * _PikminPushScale;
      }
    }

    float storedY = _Rigidbody.velocity.y;
    _Rigidbody.velocity = _MovementVector;
    _MovementVector = new Vector3(0, storedY, 0);
  }

  void OnCollisionEnter (Collision collision) {
    if (collision.gameObject.layer != LayerMask.NameToLayer ("PikminInteractable")) {
      return;
    }

    if (_TargetObjectCollider != null && collision.collider == _TargetObjectCollider) {
      CarryoutIntention ();
    }

    if (_CurrentState == PikminStates.Thrown) {
      _TargetObject = collision.transform;
      _TargetObjectCollider = collision.collider;
      _Intention = collision.gameObject.GetComponent<IPikminInteractable> ().IntentionType;
      CarryoutIntention ();
    }
  }

  #endregion

  #region States
  void CarryoutIntention () {
    PikminStates previousState = _CurrentState;

    // Run intention-specific logic (attack = OnAttackStart for the target object)
    switch (_Intention) {
      case PikminIntention.Attack:
        _AttackingTransform = _TargetObject;

        _Attacking = _TargetObject.GetComponent<IPikminAttack> ();
        _Attacking.OnAttackStart (this);

        LatchOnto (_AttackingTransform);

        ChangeState (PikminStates.Attacking);
        break;
      case PikminIntention.Carry:
        break;
      case PikminIntention.PullWeeds:
        break;
      case PikminIntention.Idle:
        ChangeState (PikminStates.Idle);
        break;
      default:
        break;
    }

    if (previousState == _CurrentState && _CurrentState != PikminStates.Idle) {
      ChangeState (PikminStates.Idle);
    }

    _Intention = PikminIntention.Idle;
  }

  void HandleIdle () {
    // Look for a target object
    Collider[] objects = Physics.OverlapSphere (transform.position, _Data._SearchRadius, _PikminInteractableMask);
    foreach (Collider collider in objects) {
      // Check if the object can even be seen by the Pikmin
      Vector3 closestPointToPikmin = collider.ClosestPoint (transform.position);
      if (Physics.Raycast (transform.position, (closestPointToPikmin - transform.position).normalized, out RaycastHit hit, _Data._SearchRadius) &&
        // See if the Collider we hit wasn't the Player OR the closest object, meaning we can't actually get to the object
        hit.collider != collider &&
        hit.transform.CompareTag ("Player") == false) {
        continue;
      }

      // We can move to the target object, and it is an interactable, so set our target object
      ChangeState (PikminStates.RunningTowards);
      _TargetObject = collider.transform;
      _TargetObjectCollider = collider;
      _Intention = collider.GetComponent<IPikminInteractable> ().IntentionType;
    }
  }

  void HandleDeath () {
    if (_InSquad) {
      RemoveFromSquad (PikminStates.Dead);
    }

    if (_RagdollTime > 0) {
      _Rigidbody.constraints = RigidbodyConstraints.None;
      _Rigidbody.isKinematic = false;
      _Rigidbody.useGravity = true;
      _RagdollTime -= Time.deltaTime;
      return;
    }

    PikminStatsManager.Remove (_Data._Colour, _CurrentMaturity, _CurrentStatSpecifier);

    AudioSource.PlayClipAtPoint (_Data._DeathNoise, transform.position, _Data._AudioVolume);

    // Create the soul gameobject, and play the death noise
    var soul = Instantiate (_DeathParticle, transform.position, Quaternion.Euler (-90, 0, 0)).GetComponent<ParticleSystem> ();
    var soulEffect = soul.main;
    soulEffect.startColor = _Data._DeathSpiritColour;
    Destroy (soul.gameObject, 5);

    // Remove the object
    Destroy (gameObject);
  }

  void HandleAttacking () {
    // The object we were attacking has died, so we can go back to being idle
    if (_AttackingTransform == null) {
      ChangeState (PikminStates.Idle);
      return;
    }

    // Maintain latch offset on object
    transform.localPosition = _LatchOffset;

    // Add to the timer and attack if we've gone past the timer
    _AttackTimer += Time.deltaTime;
    if (_AttackTimer >= _Data._AttackDelay) {
      _Attacking.OnAttackRecieve (_Data._AttackDamage);
      _AttackTimer = 0;
    }
  }

  #endregion

  #region Misc
  void MoveTowards (Vector3 position) {
    // Rotate to look at the object we're moving towards
    Vector3 delta = (position - transform.position).normalized;
    delta.y = 0;
    transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation (delta), _Data._RotationSpeed * Time.deltaTime);


    if (MathUtil.DistanceTo (transform.position, position, false) < Mathf.Pow(0.5f, 2)) {
      _CurrentMoveSpeed = Mathf.SmoothStep (_CurrentMoveSpeed, 0, 0.25f);
    }
    else
    {
      // To prevent instant, janky movement we step towards the resultant max speed according to _Acceleration
      _CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _Data._MaxMovementSpeed, _Data._AccelerationSpeed * Time.deltaTime);
    }

    Vector3 newVelocity = delta.normalized * _CurrentMoveSpeed;
    newVelocity.y = _Rigidbody.velocity.y;
    _MovementVector = newVelocity;
  }

  void MoveTowardsTarget () {
    MoveTowards (ClosestPointOnTarget ());
  }

  Vector3 ClosestPointOnTarget () {
    // Check if there is a collider for the target object we're running to
    if (_TargetObjectCollider != null) {
      // Our target is the closest point on the collider
      return _TargetObjectCollider.ClosestPoint (transform.position);
    }

    return _TargetObject.position;
  }

  #endregion

  #region Public Functions

  public void ChangeState (PikminStates newState) {
    // There's no saving pikmin from death
    if (_CurrentState == PikminStates.Dead)
      return;

    // Shrink collider and grab latch offset to maintain the same position
    if (newState == PikminStates.Attacking) {
      _Collider.radius /= 5;
      _Collider.height /= 5;
      _LatchOffset = transform.localPosition;
      if (Physics.Raycast (transform.position, (ClosestPointOnTarget () - transform.position).normalized, out RaycastHit info)) {
        transform.rotation = Quaternion.LookRotation (info.normal);
      }
    }

    if (_CurrentState == PikminStates.RunningTowards || _CurrentState == PikminStates.Idle && _TargetObject != null) {
      _TargetObject = null;
      _TargetObjectCollider = null;
    }
    else if (_CurrentState == PikminStates.Attacking) {
      // Reset latching variables aka regrow collider size and reset the latch offset
      _Collider.radius *= 5;
      _Collider.height *= 5;
      transform.rotation = Quaternion.identity;
      LatchOnto (null);
      _MovementVector = Vector3.zero;

      _AttackingTransform = null;

      // Check if the object we were attacking was still active or not
      if (_Attacking == null) {
        _LatchOffset = Vector3.zero;
        return;
      }

      // As it is still active, and not null, we can call the appropriate function
      _Attacking.OnAttackEnd (this);
      _AttackTimer = 0;
    }

    _CurrentState = newState;
  }

  public void StartThrowHold () {
    ChangeState (PikminStates.BeingHeld);
  }

  // We've been thrown!
  public void EndThrowHold (Vector3 EndPoint) {
    //TODD: put some formulas or some shit to properly move the pikmin, for now teleporting works fine
    //Replacing this with an offset throwing (when its implemented of course) would prevent the pikmin from getting caught on olimar
    transform.position = EndPoint;
    //TODO: not set pikmin to idle state, but making the thrown state would just be the arc, which isn't needed right now because teleporting
    RemoveFromSquad ();
  }

  public void StartRunTowards (Transform obj) {
    _TargetObject = obj;
    ChangeState (PikminStates.RunningTowards);
  }

  public void LatchOnto (Transform obj) {
    transform.parent = obj;
    _Rigidbody.isKinematic = (obj != null);
  }

  public void AddToSquad () {
    if (!_InSquad && _CurrentState != PikminStates.Dead) {
      _InSquad = true;
      ChangeState (PikminStates.RunningTowards);

      PikminStatsManager.AddToSquad (this, _Data._Colour, _CurrentMaturity);
      PikminStatsManager.ReassignFormation();
    }
  }

  public void RemoveFromSquad (PikminStates to = PikminStates.Idle) {
    if (_InSquad) {
      _InSquad = false;
      _TargetObject = null;
      ChangeState (to);

      PikminStatsManager.RemoveFromSquad (this, _Data._Colour, _CurrentMaturity);
      PikminStatsManager.ReassignFormation();
    }
  }

  public void Die (float ragdollTimer = 0) {
    _RagdollTime = ragdollTimer;
    ChangeState (PikminStates.Dead);
  }

  public void SetTarget (Vector3 position) {

  }

  #endregion
}

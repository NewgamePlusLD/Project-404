/*
 * WhistleController.cs
 * Created by: Ambrosia
 * Created on: 12/5/2020 (dd/mm/yy)
 */

using UnityEngine;

public class WhistleController : MonoBehaviour {
  [Header ("Components")]
  public Transform _CursorTransform = null;
  [SerializeField] ParticleSystem _ParentParticle = null;
  [SerializeField] ParticleSystem[] _WhistleParticles = null;

  [Header ("Settings")]
  [SerializeField] float _StartRadius = 0.5f;
  [SerializeField] float _MaxRadius = 3;
  // Thanks The JustGreat Minty Meeo for Pikmin 1 values on the whistle
  [SerializeField] float _ExpandBlowTime = 0.45f;
  [SerializeField] float _HoldingBlowTime = 1;
  [SerializeField] float _MaxWhistleDistance = Mathf.Infinity;
  [SerializeField] LayerMask _WhistleInteractLayer = 0;
  [SerializeField] LayerMask _WhistleBlowInteractLayer = 0;

  [Header ("Debug")]
  [SerializeField] bool _Blowing = false;
  [SerializeField] float _CurrentTime = 0;
  [SerializeField] float _CurrentRadius = 0;
  [SerializeField] float _TotalBlowTime = 0;

  AudioSource _ParentParticleAudio = null;
  Transform _PlayerTransform = null;
  Camera _MainCamera = null;
  LineRenderer _Renderer = null;

  void Start () {
    _MainCamera = Camera.main;
    _PlayerTransform = GameManager._Player.transform;

    _Renderer = GetComponent<LineRenderer> ();
    _ParentParticleAudio = _ParentParticle.GetComponent<AudioSource> ();

    _TotalBlowTime = _ExpandBlowTime + _HoldingBlowTime;

    foreach (var ps in _WhistleParticles) {
      ParticleSystem.MainModule newMain = ps.main;
      newMain.duration = _TotalBlowTime;
    }
  }

  void Update () {
    if (GameManager._IsPaused) {
      return;
    }

    _Renderer.SetPosition (0, _PlayerTransform.position);
    _Renderer.SetPosition (1, _CursorTransform.position);

    if (Input.GetButtonDown ("B Button")) {
      // Start the particles and audio
      _ParentParticle.Stop (true);
      _ParentParticle.Play (true);
      _ParentParticleAudio.Play ();

      _CurrentRadius = _StartRadius;
      _ParentParticle.transform.localScale = MathUtil.XZToXYZ (Vector2.one * _StartRadius, _StartRadius);
      _Blowing = true;
    }

    if (Input.GetButtonUp ("B Button") || _CurrentTime >= _TotalBlowTime) {
      // Stop the particles and audio
      _ParentParticle.Stop (true);
      _ParentParticleAudio.Stop ();

      _CurrentTime = 0;
      _Blowing = false;
    }

    // Move the Player 
    Ray ray = _MainCamera.ScreenPointToRay (Input.mousePosition);
    if (Physics.Raycast (ray, out RaycastHit hit, _MaxWhistleDistance, _WhistleInteractLayer, QueryTriggerInteraction.Ignore)) {
      transform.position = hit.point + Vector3.up / 1.5f;
      _CursorTransform.position = hit.point;
    }

    if (_Blowing) {
      _CurrentTime += Time.deltaTime;

      float frac = (_CurrentTime / _ExpandBlowTime);
      if (frac > 1) {
        frac = 1;
      }

      _CurrentRadius = Mathf.SmoothStep (_CurrentRadius, _MaxRadius, frac);
      _ParentParticle.transform.localScale = MathUtil.XZToXYZ (Vector2.one * _CurrentRadius / 6, _CurrentRadius / 6);

      Collider[] colliders = Physics.OverlapSphere (_ParentParticle.transform.position, _CurrentRadius, _WhistleBlowInteractLayer);
      foreach (var collider in colliders) {
        if (!collider.CompareTag ("Pikmin")) {
          continue;
        }

        collider.GetComponent<PikminAI> ().AddToSquad ();
      }
    }
  }
}

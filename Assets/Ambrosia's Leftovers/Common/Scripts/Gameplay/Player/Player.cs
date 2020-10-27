/*
 * Player.cs
 * Created by: Ambrosia
 * Created on: 20/4/2020 (dd/mm/yy)
 */

using UnityEngine;

public class Player : MonoBehaviour, IHealth {
  [Header ("Components")]
  public FormationCenter _FormationCenter = null;
  [SerializeField] AudioClip _LowHealthClip = null;
  AudioSource _AudioSource = null;

  [Header ("Settings")]
  [SerializeField] float _MaxHealth = 100;
  [SerializeField] float _LowHealthDelay = 1;

  [Header ("Formation")]
  [SerializeField] float _StartingDistance = 1;
  [SerializeField] float _DistancePerPikmin = 0.1f;

  [Header ("Debugging")]
  [SerializeField] float _CurrentHealth = 0;
  [HideInInspector] public bool _Paralyzed = false;
	//Added by Chirz
	[SerializeField] InteractorKey interactorKey;

  float _LowHealthAudioTimer = 0;

  void Awake () {
    _CurrentHealth = _MaxHealth;

    _AudioSource = GetComponent<AudioSource> ();

    // Apply singleton pattern to allow for the objects in the scene to reference the active Player instance
    if (GameManager._Player == null) {
      GameManager._Player = this;
    }
    else {
      Destroy (gameObject);
    }
  }

  void Update () {
    // Tab pauses the game, experimental
    if (Application.isEditor) {
      if (Input.GetKeyDown (KeyCode.Tab)) {
        GameManager._IsPaused = !GameManager._IsPaused;
        Time.timeScale = GameManager._IsPaused ? 0 : 1;
      }

      if (Input.GetKeyDown (KeyCode.Alpha3)) {
        SubtractHealth (_MaxHealth / 10);
      }
    }

    if (_CurrentHealth <= 0) {
      Debug.Log ("Player is dead!");
      Debug.Break ();
    }

    // If the health is less that a quarter of what the max is, then play the low health audio
    if (_CurrentHealth / _MaxHealth < 0.25f) {
      _LowHealthAudioTimer += Time.deltaTime;
      if (_LowHealthAudioTimer >= _LowHealthDelay) {
        _LowHealthAudioTimer = 0;
        _AudioSource.PlayOneShot (_LowHealthClip);
      }
    }

    Vector3 targetPosition = _FormationCenter.transform.position - transform.position;
    _FormationCenter.transform.position =
      transform.position + Vector3.ClampMagnitude (targetPosition, _StartingDistance + _DistancePerPikmin * PikminStatsManager.GetTotalInSquad ());
  
	//Added by Chirz
		if(!_Paralyzed)interactorKey.action = Input.GetKeyDown (KeyCode.Space);

	
	}

  #region Health Implementation

  public float GetCurrentHealth () {
    return _CurrentHealth;
  }

  public float GetMaxHealth () {
    return _MaxHealth;
  }

  public void SetHealth (float h) {
    _CurrentHealth = h;
  }

  public float AddHealth (float h) {
    _CurrentHealth += h;
    return _CurrentHealth;
  }

  public float SubtractHealth (float h) {
    _CurrentHealth -= h;
    return h;
  }

  #endregion
}

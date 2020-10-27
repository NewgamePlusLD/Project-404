/*
 * DayController.cs
 * Created by: Ambrosia
 * Created on: 23/5/2020 (dd/mm/yy)
 */

using UnityEngine;

public class DayController : MonoBehaviour {
  [Header ("Debugging")]
  [SerializeField] DayState _State = DayState.Morning;
  [SerializeField] float _currentTime = 0;
  [SerializeField] Transform sunRotator = null;
  [SerializeField] AnimationCurve sunHeight = null;

  public float _CurrentTime => _currentTime;

  void Awake () {
    GameManager._DayController = this;
  }

  void Update () {
    if (GameManager._IsPaused || GameManager._Player._Paralyzed)
    {
      return;
    }

    _currentTime += Time.deltaTime / (30 * 60);

    sunRotator.eulerAngles = new Vector3 (sunHeight.Evaluate (_currentTime), (360 * _currentTime) - 90, 0);
  }

  void OnDrawGizmosSelected () {
    sunRotator.eulerAngles = new Vector3 (sunHeight.Evaluate (_currentTime), (360 * _currentTime) - 90, 0);
  }
}

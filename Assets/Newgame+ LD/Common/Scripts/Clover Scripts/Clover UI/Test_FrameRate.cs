/*
 * Test_FrameRate.cs
 * Created by: ???
 * Created on: ??/??/???? (dd/mm/yy)
 * 
 * I cannot remember where I originally found this -Newgame+ LD
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Test_FrameRate : MonoBehaviour {
  public int avgFrameRate;
  public Text textfield;

  public bool accumulationMode;
  public int accumulation;
  public float timeLeft;

  void Update () {
    if (!accumulationMode) {
      float current = 0;
      current = Time.frameCount / Time.unscaledTime;
      avgFrameRate = (int) current;
      textfield.text = (avgFrameRate + " FPS");

    }
    else {

      if (timeLeft > 0) {

        accumulation++;
        timeLeft -= Time.unscaledDeltaTime;

      }
      else {

        textfield.text = (accumulation + " FPS");
        timeLeft = 1;
        accumulation = 0;
      }

    }
  }

}

/*
 * AudioLoopSeconds.cs
 * Created by: ??? (Is from Gistix's Sonic '06 PC)
 * Created on: ??/??/???? (dd/mm/yy)
 * 
 * Edited AudioLoopSample.cs by Newgame+ LD to use Seconds instead of Samples
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLoopSeconds : MonoBehaviour {

  public AudioSource source;
  public AudioClip clip;

  public float loopStart;
  public float loopEnd;

  // Use this for initialization
  void Start () {
    StartCoroutine (PlayAndLoopClip (clip));
  }

  IEnumerator PlayAndLoopClip (AudioClip pointerClip) {
    source.clip = pointerClip;
    source.Play ();

    while (true) {
      if (source.time > loopEnd)
        source.time = loopStart;

      yield return null;
    }
  }
}

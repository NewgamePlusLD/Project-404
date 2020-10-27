/*
 * InteractorKey.cs
 * Created by: Newgame+ LD
 * Created on: ??/??/???? (dd/mm/yy)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractorKey : MonoBehaviour {

  public InteractibleEvent eventTarget;
  public bool action;


	void Update ()	{
		if(action)	{
			eventTarget?.m_actionEvent.Invoke(this);
		}

		action = false;
	}

}

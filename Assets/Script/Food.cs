using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Food : MonoBehaviour {

	public bool Active{set;get;}
	private const float gravity = 2.0f;
	private float speed; 
	private float verticalVelocity;
	private bool isSliced = false;


	public void FoodLauncher(float verticalVelocity, float xSpeed, float xStart)
	{
		Active = true;
		speed = xSpeed;
		this.verticalVelocity = verticalVelocity;
		transform.position = new Vector3 (xStart, 0, 0);
		isSliced = false;
	}

	private void Update()
	{
		if (!Active)
			return;
		verticalVelocity -= gravity * Time.deltaTime;
		transform.position += new Vector3 (speed, verticalVelocity, 0) * Time.deltaTime;

		if (transform.position.y < -1) {
			Active = false;
			/* if (!isSliced)
				Manager.Instance.LoseLP (); */
		}
	}

	public void Slice()
	{
		if (isSliced) 
			return;
		if (verticalVelocity < 0.5f) 
			verticalVelocity = 0.5f;
		speed=speed*0.5f;
		isSliced = true;

		Manager.Instance.IncrementScore (1);
	}



}

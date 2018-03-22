using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

public class Manager : MonoBehaviour {
	private List<Food> food = new List<Food>();
	public GameObject foodPrefab;
	public GameObject pauseMenu;
	public GameObject deathMenu;
	GameObject[] killEmAll;
	private Collider2D[] foodCols;
	private float deltaSpawn=1.0f;
	private float lastSpawn;
	private int scoreCount;
	private int highScore;
	private int lifePoint;
	public UnityEngine.UI.Text scoreText;
	public UnityEngine.UI.Text highscoreText;
	public UnityEngine.UI.Text toggleText;
	public Image[] points;
	public static Manager Instance;
	private bool isPaused = false;
	int kill=0;
	private MergeHandAndAR handScript;
	public Camera handCamera;


	private void Awake()
	{
		Instance = this;
	}
		
	private void Start()
	{
		foodCols = new Collider2D[0];
		NewGame ();
		StartCoroutine ("Wait");
		handScript = handCamera.GetComponent<MergeHandAndAR> ();

	}

	public void NewGame()
	{
		scoreCount = 0;
		lifePoint = 3;
		pauseMenu.SetActive (false);
		scoreText.text = scoreCount.ToString ();
		highScore = PlayerPrefs.GetInt ("Score");
		highscoreText.text = "Best: " + highScore.ToString (); 
		Time.timeScale = 1;
		isPaused = false;
		deathMenu.SetActive (false);
		foreach (Image i in points)
			i.enabled = true;
		foreach (Food f in food) {
			Destroy (f.gameObject);
			food.Clear ();
		}
			
	
	}	

	IEnumerator Wait()
	{
		while (enabled)
		{
			yield return new WaitForSeconds (1.2f); {
			killEmAll = GameObject.FindGameObjectsWithTag ("Vegetable");
				if (handScript.pointaX >-1.85 && handScript.pointbX < 1.85 && handScript.pointaY > 0 && handScript.pointbY < 2)
				for (int i=0; i<killEmAll.Length; i++) {
				Destroy (killEmAll [i].gameObject);
				kill++;
				Instance.IncrementScore (1);
			}
		} 
	}
} 
	private void Update ()
	{
		if (Input.GetMouseButton (0))
			toggleText.enabled = false;
		if (isPaused)
			return;
		if (Time.time - lastSpawn > deltaSpawn) {
			Food f = getFood ();
			float randomX = Random.Range (-1.65f, 1.65f);
			f.FoodLauncher (Random.Range (1.85f, 2.75f), randomX, -randomX);
			lastSpawn = Time.time;
		}
		handScript.pointaX =  ((800 - handScript.pointaX) / 370); // pri interval (-1.85, 1.85) i uvelichavane s 0.01 imame 370 varianta
		handScript.pointbX = ((800 - handScript.pointbX) / 370);
		handScript.pointaY = ((480 - handScript.pointaY) / 400);
		handScript.pointbY = ((480 - handScript.pointbY) / 400);
	}
		

	private Food getFood() 
	{
		Food f = food.Find (x => !x.Active);
		if (f == null) {
			f = Instantiate (foodPrefab).GetComponent<Food> ();
			food.Add (f);
		}
		return f;
	}

	public void IncrementScore (int scoreAmount)
	{
		scoreCount += scoreAmount;
		scoreText.text = scoreCount.ToString ();

		if (scoreCount > highScore) {
			highScore = scoreCount;
			highscoreText.text = "BEST:" + highScore.ToString (); 
			PlayerPrefs.SetInt ("Score", highScore);
		}
	} 

	// shte bude dobaveno sled vuforia update
	/* public void LoseLP() 
	{
		if (lifePoint == 0) {
			return;
		}
		lifePoint--;
		points[lifePoint].enabled = false;
		if (lifePoint == 0)
			Death ();
	}

	public void Death()
	{
		isPaused = true;
		deathMenu.SetActive (true);
	}   */

	public void PauseGame()
	{
		pauseMenu.SetActive (!pauseMenu.activeSelf);
		isPaused = pauseMenu.activeSelf;
		Time.timeScale = (Time.timeScale == 0) ? 1: 0 ;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SongSelect : MonoBehaviour
{
	public Session session;
	public List<ScanSong.SongInfo> songs;
	public SongBlock songblockPrefab;
	public GameObject selectScreen;
	public ResultScore resultScore;
	/**
	 * Texture를 사용하여 이미지를 표시 합니다.
	 */
	public RawImage fade;
	public void Awake()
	{

		songs = ScanSong.allSongs;
		/**
		 * 노래가 없다면 첫번째 Scene( Scan Scene ) 으로 전환합니다.
		 */
		if (songs == null)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
			return;
		}

		/**
		 * 점점 투명해 진다.
		 */
		fade.color = new Color(0, 0, 0, 1);
		StartCoroutine(FadeOutStart());

		/**
		 * 완전 투명해 지면 selectScreen 활성화
		 */
		selectScreen.SetActive(true);

		for (int i = 0; i < songs.Count; ++i)
		{
			// SongBlock 프리팹으로 SongBlock Clone 생성
			SongBlock newBlock = Instantiate(songblockPrefab.gameObject).GetComponent<SongBlock>();

			// SongBlock 프래팹의 부모를 Clone에게도 똑같이 부모를 설정해 준다.
			newBlock.transform.SetParent(songblockPrefab.transform.parent);
			newBlock.transform.localPosition = Vector3.zero;
			newBlock.transform.localScale = Vector3.one;
			newBlock.text.text = songs[i].displayArtist;
			newBlock.fileInfo = songs[i].fileInfo;
		}
		

		/**
		 * 노래가 없다면 SongBlock 프리팹 버튼 비활성화
		 * No Songs found 출력
		 */
		if (songs.Count == 0)
		{
			songblockPrefab.text.text = "No Songs found";
			songblockPrefab.GetComponent<Button>().enabled = false;
		}
		/**
		 * 노래가 존재한다면 SongBock 프리팹 자체를 비활성화
		 */
		else
		{
			songblockPrefab.gameObject.SetActive(false);
		}
	}

	

	private IEnumerator FadeOutStart()
	{
		/**
		 * Color의 Alpha 값은 0 ~ 1 까지의 값
		 * 0에 가까울수록 투명하고 1에 가까울수록 불투명
		 * 아래는 점점 투명해 지는 것.
		 */
		while (fade.color.a > 0)
		{
			// 다음 프레임 동작 되기 전까지 대기
			yield return null;
			fade.color -= new Color(0, 0, 0, Time.deltaTime);
		}
		fade.gameObject.SetActive(false);
	}

	private IEnumerator FadeInStart()
	{
		while (fade.color.a < 1)
		{
			// 다음 프레임 동작 되기 전까지 대기
			yield return null;
			fade.color += new Color(0, 0, 0, Time.deltaTime);
		}
		fade.gameObject.SetActive(true);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (session.playing)
			{
				session.playing = false;
				StartCoroutine(EndingSong());
			
			}
		}
	}
	/**
	 * 음악을 선택하면 Song Load
	 */
	public void LoadSong(FileInfo chartFile)
	{
		StartCoroutine(StartingSong(chartFile));
	}

	private IEnumerator StartingSong(FileInfo chartFile)
	{
		
		Debug.Log("Loading " + chartFile.FullName);
		Song song = null;

		/**
		 * 해당 파일에 대한 Song 객체 Callback 받기
		 */
		SongLoader.Instance.Load(chartFile.FullName, delegate (Song _song)
		{
			song = _song;
		});

		/**
		 * fade 활성화
		 * fade를 다시 불투명하게 만든다.
		 */
		fade.color = new Color(0, 0, 0, 0);
		fade.gameObject.SetActive(true);
		while (fade.color.a < 1)
		{
			fade.color += new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		// song이 완성되기 전까지 계속 대기
		while (song == null) {
			yield return null;
		}

		/**
		 * 오디오 파일을 불러온다.
		 */
		Debug.Log("Loading audio");
		bool prepared = false;

		SongLoader.Instance.PrepareAudio(song, delegate ()
		{
			prepared = true;
		});

		// 오디오 파일이 준비가 완료 될때까지 대기
		while (!prepared) yield return null;

		/**
		 * 난이도 Easy, Medium, Hard, Expert 생성
		 */
		Session.PlayerInfo[] players = new Session.PlayerInfo[]
		{
			//new Session.PlayerInfo(Song.Difficulty.Easy),
			//new Session.PlayerInfo(Song.Difficulty.Medium),
			//new Session.PlayerInfo(Song.Difficulty.Hard),
			new Session.PlayerInfo(Song.Difficulty.Expert)
		};

		/**
		 * session 초기화
		 */
		session.Initialize(song, players);

		/**
		 * 노래 선택창 비활성화
		 */
		selectScreen.SetActive(false);


		Debug.Log("Ready to play");

		/**
		 * fade를 점점 투명하게 만든다
		 */
		while (fade.color.a > 0)
		{
			fade.color -= new Color(0, 0, 0, Time.deltaTime);
			// 다음 프레임 동작할 때까지 대기
			yield return null;
		}
		fade.gameObject.SetActive(false);
		System.GC.Collect();

		/**
		 * Session Start
		 */
		session.StartPlaying();
		
	}

	public IEnumerator EndingSong()
	{

		/**
		 * fade 투명한것으로 활성화
		 */
		fade.color = new Color(0, 0, 0, 0);
		fade.gameObject.SetActive(true);

		/**
		 * 불투명하게 변경
		 */
		while (fade.color.a < 1)
		{
			fade.color += new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		/**
		 * 결과창
		 */
		resultScore.ShowResult();

		/**
		 * Session종료
		 */
		session.EndSession();

		/**
		 * 노래 선택창 활성화
		 */
		//selectScreen.SetActive(true);

		/**
		 * fade를 다시 투명화 시키기
		 */
		//while (fade.color.a > 0)
		//{
		//	fade.color -= new Color(0, 0, 0, Time.deltaTime);
		//	yield return null;
		//}
		//fade.gameObject.SetActive(false);
	}
	public void activateScreen()
	{
		// 화면 활성화
		selectScreen.SetActive(true);
		/**
		 * 점점 투명하게
		 */
		StartCoroutine(FadeOutStart());
	}

	public void inActivateScreen()
	{
		// 화면 활성화
		selectScreen.SetActive(false);
		/**
		 * 점점 투명하게
		 */
		StartCoroutine(FadeInStart());
	}
}

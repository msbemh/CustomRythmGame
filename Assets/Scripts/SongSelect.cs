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
	 * Texture�� ����Ͽ� �̹����� ǥ�� �մϴ�.
	 */
	public RawImage fade;
	public void Awake()
	{

		songs = ScanSong.allSongs;
		/**
		 * �뷡�� ���ٸ� ù��° Scene( Scan Scene ) ���� ��ȯ�մϴ�.
		 */
		if (songs == null)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
			return;
		}

		/**
		 * ���� ������ ����.
		 */
		fade.color = new Color(0, 0, 0, 1);
		StartCoroutine(FadeOutStart());

		/**
		 * ���� ������ ���� selectScreen Ȱ��ȭ
		 */
		selectScreen.SetActive(true);

		for (int i = 0; i < songs.Count; ++i)
		{
			// SongBlock ���������� SongBlock Clone ����
			SongBlock newBlock = Instantiate(songblockPrefab.gameObject).GetComponent<SongBlock>();

			// SongBlock �������� �θ� Clone���Ե� �Ȱ��� �θ� ������ �ش�.
			newBlock.transform.SetParent(songblockPrefab.transform.parent);
			newBlock.transform.localPosition = Vector3.zero;
			newBlock.transform.localScale = Vector3.one;
			newBlock.text.text = songs[i].displayArtist;
			newBlock.fileInfo = songs[i].fileInfo;
		}
		

		/**
		 * �뷡�� ���ٸ� SongBlock ������ ��ư ��Ȱ��ȭ
		 * No Songs found ���
		 */
		if (songs.Count == 0)
		{
			songblockPrefab.text.text = "No Songs found";
			songblockPrefab.GetComponent<Button>().enabled = false;
		}
		/**
		 * �뷡�� �����Ѵٸ� SongBock ������ ��ü�� ��Ȱ��ȭ
		 */
		else
		{
			songblockPrefab.gameObject.SetActive(false);
		}
	}

	

	private IEnumerator FadeOutStart()
	{
		/**
		 * Color�� Alpha ���� 0 ~ 1 ������ ��
		 * 0�� �������� �����ϰ� 1�� �������� ������
		 * �Ʒ��� ���� ������ ���� ��.
		 */
		while (fade.color.a > 0)
		{
			// ���� ������ ���� �Ǳ� ������ ���
			yield return null;
			fade.color -= new Color(0, 0, 0, Time.deltaTime);
		}
		fade.gameObject.SetActive(false);
	}

	private IEnumerator FadeInStart()
	{
		while (fade.color.a < 1)
		{
			// ���� ������ ���� �Ǳ� ������ ���
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
	 * ������ �����ϸ� Song Load
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
		 * �ش� ���Ͽ� ���� Song ��ü Callback �ޱ�
		 */
		SongLoader.Instance.Load(chartFile.FullName, delegate (Song _song)
		{
			song = _song;
		});

		/**
		 * fade Ȱ��ȭ
		 * fade�� �ٽ� �������ϰ� �����.
		 */
		fade.color = new Color(0, 0, 0, 0);
		fade.gameObject.SetActive(true);
		while (fade.color.a < 1)
		{
			fade.color += new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		// song�� �ϼ��Ǳ� ������ ��� ���
		while (song == null) {
			yield return null;
		}

		/**
		 * ����� ������ �ҷ��´�.
		 */
		Debug.Log("Loading audio");
		bool prepared = false;

		SongLoader.Instance.PrepareAudio(song, delegate ()
		{
			prepared = true;
		});

		// ����� ������ �غ� �Ϸ� �ɶ����� ���
		while (!prepared) yield return null;

		/**
		 * ���̵� Easy, Medium, Hard, Expert ����
		 */
		Session.PlayerInfo[] players = new Session.PlayerInfo[]
		{
			//new Session.PlayerInfo(Song.Difficulty.Easy),
			//new Session.PlayerInfo(Song.Difficulty.Medium),
			//new Session.PlayerInfo(Song.Difficulty.Hard),
			new Session.PlayerInfo(Song.Difficulty.Expert)
		};

		/**
		 * session �ʱ�ȭ
		 */
		session.Initialize(song, players);

		/**
		 * �뷡 ����â ��Ȱ��ȭ
		 */
		selectScreen.SetActive(false);


		Debug.Log("Ready to play");

		/**
		 * fade�� ���� �����ϰ� �����
		 */
		while (fade.color.a > 0)
		{
			fade.color -= new Color(0, 0, 0, Time.deltaTime);
			// ���� ������ ������ ������ ���
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
		 * fade �����Ѱ����� Ȱ��ȭ
		 */
		fade.color = new Color(0, 0, 0, 0);
		fade.gameObject.SetActive(true);

		/**
		 * �������ϰ� ����
		 */
		while (fade.color.a < 1)
		{
			fade.color += new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		/**
		 * ���â
		 */
		resultScore.ShowResult();

		/**
		 * Session����
		 */
		session.EndSession();

		/**
		 * �뷡 ����â Ȱ��ȭ
		 */
		//selectScreen.SetActive(true);

		/**
		 * fade�� �ٽ� ����ȭ ��Ű��
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
		// ȭ�� Ȱ��ȭ
		selectScreen.SetActive(true);
		/**
		 * ���� �����ϰ�
		 */
		StartCoroutine(FadeOutStart());
	}

	public void inActivateScreen()
	{
		// ȭ�� Ȱ��ȭ
		selectScreen.SetActive(false);
		/**
		 * ���� �����ϰ�
		 */
		StartCoroutine(FadeInStart());
	}
}

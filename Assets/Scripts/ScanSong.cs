using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScanSong : MonoBehaviour
{
    public static List<SongInfo> allSongs;

	public delegate void OnFinished(List<SongInfo> songs);

	public Image loadingImage;
	private List<SongInfo> songs = null;

	/**
	 * 동기화 작업을 할때 사용할 오브젝트
	 */
	System.Object lockObject = new System.Object();

	/**
	 * 노래 정보
	 */
	[System.Serializable]
    public class SongInfo
    {
        public FileInfo fileInfo;
        public string artist, name, displayArtist, displayName;
    }

    void Start()
    {
		/**
		 * 애플리케이션의 백그라운드 로딩 우선 순위를 낮추어서
		 * 메인 스레드가 중요한 작업을 작업하는데 지장이 없도록 합니다.
		 */
        Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;

		/**
		 * delegate(){} 형식의 parameter는 Callback이라고 생각 하시면 됩니다.
		 * 노래 목록들을 가지고 오는 작업
		 */
        StartCoroutine(ScanAndContinue(delegate (List<SongInfo> songs)
        {
            allSongs = songs;
        }));
    }

	private IEnumerator ScanAndContinue(OnFinished onFinished)
	{
		/**
		 * Game Scene 을 로드합니다.
		 * 단, 비활성화
		 */
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
		asyncLoad.allowSceneActivation = false;

		/**
		 * 노래 파일들을 찾는 스레드
		 */
		Thread thread = new Thread(ScanForSongsRecursively);
		/**
		 * 백그라운드 설정은 메인 스레드가 종료되더라도 계속해서 실행하게 됩니다.
		 * 단, UI 업데이트와 같은 작업은 메인 스레드에서 실행해야 합니다.
		 */
		thread.IsBackground = true;
		/**
		 * Application.dataPath는 Assets 폴더를 가리키는 상대 경로입니다.
		 * DirectoryInfo 객체를 사용하면 해당 경로의 파일 및 폴더와 관련된 작업을 수행할 수 있습니다.
		 */
		thread.Start(new DirectoryInfo(Application.dataPath).Parent);

		/**
		 * songs를 얻어 올때까지 계속 기다린다.
		 */
		while (true)
		{
			/**
			 * 무한 while문에서 다음 프레임 까지 일시중단
			 */
			yield return null;
			lock (lockObject)
			{
				if (songs != null) break;
			}
		}
		// 스레드 중단
		thread.Abort();


		/**
		 * Game Scene이 로딩 완료 될 동안 대기한다.
		 */
		allSongs = songs;
		while (asyncLoad.isDone)
		{
			Debug.Log("Still loading scene: " + asyncLoad.progress);
			if (asyncLoad.progress >= 0.9) break;
			/**
			 * 무한 while문에서 다음 프레임 까지 일시중단
			 */
			yield return null;
		}

		/**
		 * 로딩 이미지를 천천히 투명화 시킨다.
		 */
		while (loadingImage.color.a > 0)
		{
			loadingImage.color -= new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		// Game Scene 활성화
		asyncLoad.allowSceneActivation = true;
	}

	/**
	 * 노래 찾기
	 * folder는 Assets 폴더의 상위
	 */
	private void ScanForSongsRecursively(object folder)
	{
		List<SongInfo> list = new List<SongInfo>();
		List<DirectoryInfo> foldersToScan = new List<DirectoryInfo>();

		foldersToScan.Add((DirectoryInfo)folder);

		while (foldersToScan.Count > 0)
		{
			DirectoryInfo[] currentScan = foldersToScan.ToArray();
			foldersToScan.Clear();
			/**
			 * 모든 폴더를 for 돌린다.
			 */
			for (int i = 0; i < currentScan.Length; ++i)
			{
				/**
				 * 폴더안에 모든 파일들을 검사한다.
				 */
				foreach (FileInfo f in currentScan[i].GetFiles())
				{
					/**
					 * song.ini 파일이 있을 경우에만
					 * 안에 있는 모든 파일들을 이용하여 songInfo를 만든다.
					 */
					if (f.Name == "song.ini")
					{
						list.Add(CreateSongInfo(currentScan[i]));
						break;
					}
				}
				/**
				 * 폴더의 하위 폴더를 추가하여 하위 파일들을 모두 검사할 수 있도록 한다.
				 */
				foreach (DirectoryInfo d in currentScan[i].GetDirectories())
				{
					foldersToScan.Add(d);
				}
			}
		}
		List<SongInfo> sortedList = Sort(list);
		lock (lockObject)
		{
			songs = sortedList;
		}
	}

	private List<SongInfo> Sort(List<SongInfo> songs)
	{
		Dictionary<string, SongInfo> songByArtists = new Dictionary<string, SongInfo>();
		List<string> artists = new List<string>();
		for (int i = 0; i < songs.Count; ++i)
		{
			if (!songByArtists.ContainsKey(songs[i].displayArtist))
			{
				artists.Add(songs[i].displayArtist);
				songByArtists.Add(songs[i].displayArtist, songs[i]);
			}
		}
		artists.Sort();
		List<SongInfo> sortedList = new List<SongInfo>();
		for (int i = 0; i < artists.Count; ++i)
		{
			sortedList.Add(songByArtists[artists[i]]);
		}
		return sortedList;
	}

	/**
	 * SongInfo를 생성한다.
	 */
	private SongInfo CreateSongInfo(DirectoryInfo folder)
	{
		SongInfo songInfo = new SongInfo();

		/**
		 * notes.chart 파일을 추가
		 */
		foreach (FileInfo f in folder.GetFiles())
		{
			if (f.Name == "notes.chart")
			{
				songInfo.fileInfo = f;
				break;
			}
		}

		/**
		 * song.ini 파일을 검색하여
		 * 필요한 정보들을 모두 가져온다.
		 * artist, name, displayArtist, displayName 정보 추출
		 */
		FileInfo ini = null;
		foreach (FileInfo f in folder.GetFiles())
		{
			if (f.Name == "song.ini")
			{
				ini = f;
				break;
			}
		}
		string[] lines = File.ReadAllLines(ini.FullName);
		for (int i = 0; i < lines.Length; ++i)
		{
			if (lines[i].StartsWith("name")) songInfo.name = lines[i].Split("="[0])[1].Trim();
			if (lines[i].StartsWith("artist")) songInfo.artist = lines[i].Split("="[0])[1].Trim();
		}
		songInfo.displayArtist = songInfo.artist + " - " + songInfo.name;
		songInfo.displayName = songInfo.name + " - " + songInfo.artist;
		return songInfo;
	}

}

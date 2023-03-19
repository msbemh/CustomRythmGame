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
	 * ����ȭ �۾��� �Ҷ� ����� ������Ʈ
	 */
	System.Object lockObject = new System.Object();

	/**
	 * �뷡 ����
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
		 * ���ø����̼��� ��׶��� �ε� �켱 ������ ���߾
		 * ���� �����尡 �߿��� �۾��� �۾��ϴµ� ������ ������ �մϴ�.
		 */
        Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;

		/**
		 * delegate(){} ������ parameter�� Callback�̶�� ���� �Ͻø� �˴ϴ�.
		 * �뷡 ��ϵ��� ������ ���� �۾�
		 */
        StartCoroutine(ScanAndContinue(delegate (List<SongInfo> songs)
        {
            allSongs = songs;
        }));
    }

	private IEnumerator ScanAndContinue(OnFinished onFinished)
	{
		/**
		 * Game Scene �� �ε��մϴ�.
		 * ��, ��Ȱ��ȭ
		 */
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
		asyncLoad.allowSceneActivation = false;

		/**
		 * �뷡 ���ϵ��� ã�� ������
		 */
		Thread thread = new Thread(ScanForSongsRecursively);
		/**
		 * ��׶��� ������ ���� �����尡 ����Ǵ��� ����ؼ� �����ϰ� �˴ϴ�.
		 * ��, UI ������Ʈ�� ���� �۾��� ���� �����忡�� �����ؾ� �մϴ�.
		 */
		thread.IsBackground = true;
		/**
		 * Application.dataPath�� Assets ������ ����Ű�� ��� ����Դϴ�.
		 * DirectoryInfo ��ü�� ����ϸ� �ش� ����� ���� �� ������ ���õ� �۾��� ������ �� �ֽ��ϴ�.
		 */
		thread.Start(new DirectoryInfo(Application.dataPath).Parent);

		/**
		 * songs�� ��� �ö����� ��� ��ٸ���.
		 */
		while (true)
		{
			/**
			 * ���� while������ ���� ������ ���� �Ͻ��ߴ�
			 */
			yield return null;
			lock (lockObject)
			{
				if (songs != null) break;
			}
		}
		// ������ �ߴ�
		thread.Abort();


		/**
		 * Game Scene�� �ε� �Ϸ� �� ���� ����Ѵ�.
		 */
		allSongs = songs;
		while (asyncLoad.isDone)
		{
			Debug.Log("Still loading scene: " + asyncLoad.progress);
			if (asyncLoad.progress >= 0.9) break;
			/**
			 * ���� while������ ���� ������ ���� �Ͻ��ߴ�
			 */
			yield return null;
		}

		/**
		 * �ε� �̹����� õõ�� ����ȭ ��Ų��.
		 */
		while (loadingImage.color.a > 0)
		{
			loadingImage.color -= new Color(0, 0, 0, Time.deltaTime);
			yield return null;
		}

		// Game Scene Ȱ��ȭ
		asyncLoad.allowSceneActivation = true;
	}

	/**
	 * �뷡 ã��
	 * folder�� Assets ������ ����
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
			 * ��� ������ for ������.
			 */
			for (int i = 0; i < currentScan.Length; ++i)
			{
				/**
				 * �����ȿ� ��� ���ϵ��� �˻��Ѵ�.
				 */
				foreach (FileInfo f in currentScan[i].GetFiles())
				{
					/**
					 * song.ini ������ ���� ��쿡��
					 * �ȿ� �ִ� ��� ���ϵ��� �̿��Ͽ� songInfo�� �����.
					 */
					if (f.Name == "song.ini")
					{
						list.Add(CreateSongInfo(currentScan[i]));
						break;
					}
				}
				/**
				 * ������ ���� ������ �߰��Ͽ� ���� ���ϵ��� ��� �˻��� �� �ֵ��� �Ѵ�.
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
	 * SongInfo�� �����Ѵ�.
	 */
	private SongInfo CreateSongInfo(DirectoryInfo folder)
	{
		SongInfo songInfo = new SongInfo();

		/**
		 * notes.chart ������ �߰�
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
		 * song.ini ������ �˻��Ͽ�
		 * �ʿ��� �������� ��� �����´�.
		 * artist, name, displayArtist, displayName ���� ����
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

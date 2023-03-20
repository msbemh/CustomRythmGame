using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Session : MonoBehaviour
{
	public Song song;
	public Player playerPrefab;
	public Player[] players;
	public SessionRenderer sessionRenderer;
	public Smoothing smoothing;
	public NoteRenderer noteRenderer;
	public bool playing = false;
	public float speed; //meter per second
	public GameObject[] prefabs;
	public GameObject[] barPrefabs;
	public int frameIndex = 0;
	public int syncIndex = 0;
	public float boardLength = 10; //meters
								   //public NoteInstance[] noteInstancePool;
	private AudioSource guitarSource, rhythmSource, songSource;
	public float time, previousTime;
	public double visualOffset;
	public double tick = 0;
	public double smoothTick = 0;
	public double starPowerDuration = 0;
	public double bpm, smoothBpm;
	public float RenderingFadeDistance = 3;
	public float RenderingFadeAmount = 1;
	public GameObject songSelect;

	public ResultScore resultScore;

	public class PlayerInfo
	{
		public PlayerInfo(Song.Difficulty _difficulty)
		{
			difficulty = _difficulty;
		}
		public Song.Difficulty difficulty;
	}

	/**
	 * �뷡�� ���̵� �ʱ�ȭ
	 */
	public void Initialize(Song _song, PlayerInfo[] _playerInfos)
	{
		Debug.Log("initializing ");
		song = _song;

		/**
		 * Audio Source ��Ÿ, ����, �뷡 ����
		 */
		guitarSource = gameObject.AddComponent<AudioSource>();
		rhythmSource = gameObject.AddComponent<AudioSource>();
		songSource = gameObject.AddComponent<AudioSource>();

		// ��Ÿ, ����, �뷡 ��� OnAwake ��Ȱ��ȭ
		guitarSource.playOnAwake = rhythmSource.playOnAwake = songSource.playOnAwake = false;

		/**
		 * ��Ÿ, ����, �뷡 clip ����
		 */
		guitarSource.clip = song.audio.guitar;
		rhythmSource.clip = song.audio.rhythm;
		songSource.clip = song.audio.song;

		/**
		 * ���� ��� ����
		 */
		guitarSource.loop = false;
		rhythmSource.loop = false;
		songSource.loop = false;

		/**
		 * Unity �������� ��� ���̴����� ����� �� �ִ� ���� ������ ����
		 */
		Shader.SetGlobalFloat("_GH_Distance", RenderingFadeDistance);
		Shader.SetGlobalFloat("_GH_Fade", RenderingFadeAmount);

		/**
		 * smoothing ����
		 */
		smoothing = new Smoothing(visualOffset);

		/**
		 * RenderTexture�� ���� ������Ʈ�� �������Ͽ� �ؽ�ó�� ���� �� �ֽ��ϴ�.
		 * 1. RenderTexture�� ����
		 * 2. ī�޶��� targetTexture�� ����
		 * 3. ī�޶� ������ ��ŸƮ
		 * 4. Texture2D ����
		 * 5. RenderTexture Ȱ��ȭ
		 * 6. Textrue2D Read �ȼ�
		 * 7. RenderTexture ��Ȱ��ȭ
		 */
		List<RenderTexture> outputs = new List<RenderTexture>();

		// �÷��̾� ����
		players = new Player[_playerInfos.Length];

		for (int i = 0; i < _playerInfos.Length; ++i)
		{
			// Player ���������� Clone ��ü ����
			players[i] = Instantiate(playerPrefab.gameObject).GetComponent<Player>();
			// Player Clone ��ü�� �θ� Session���� �д�.
			players[i].transform.SetParent(transform);
			// Player Clone ��ü�� Ȱ��ȭ
			players[i].gameObject.SetActive(true);
		}

		//XInput.SetActivePlayerCount(_playerInfos.Length);

		for (int i = 0; i < players.Length; ++i)
		{
			/**
			 * ��� Pool�� �������ִ� ��ü ����
			 * noteSize, barSize, noteInstanceSize, (������)
			 * NoteModel[][] note, NoteInstance[] noteInstance, BarInstance[] bar (��ü�� ��� �迭)
			 * �� ���� ������ ���� ��ü
			 * ������ ����
			 */
			Player.Pool pool = new Player.Pool();
			pool.barSize = 64;
			pool.noteInstanceSize = 1024;
			pool.noteSize = 256;

			/**
			 * Pool Index ����
			 * note, int[] noteModel, bar, noteInstance�� ���� Index ����
			 */
			Player.PoolIndex poolIndex = new Player.PoolIndex();

			/**
			 * Note Instance ����
			 * NoteModel noteModel (��ü),
		     * timestamp, seen, star, hammeron, fred, duration�� ���� ������ ���� ��ü
			 */
			pool.noteInstance = new Player.NoteInstance[pool.noteInstanceSize];
			for (int j = 0; j < pool.noteInstanceSize; ++j)
			{
				pool.noteInstance[j] = new Player.NoteInstance();
			}

			/**
			 * Note�� Pool�� ����
			 * �ش� Pool �ȿ��� �̸� Note���� ������ ���´�.
			 * ���� prefabs(�� �� �� �� �� ��Ʈ)���� pool�� ���� �����Ѵ�
			 * 
			 * [NoteModel]
			 * Transform myTransform
			 * SpriteRenderer spriteRenderer
			 * MeshRenderer line
			 * Material materialInstance
			 * 
			 * �� index�� Pool�� �����ϰ� �ȴ�.
			 */
			pool.note = new NoteModel[prefabs.Length][];
			for (int j = 0; j < prefabs.Length; ++j)
			{
				pool.note[j] = players[i].MakePool(pool.noteSize, prefabs[j]);
			}

			/**
			 * Bar Instance ����
			 * timestamp,
			 * Transform myTransform
			 */
			pool.bar = new BarInstance[pool.barSize];

			/**
			 * pool Index�� 0���� �ʱ�ȭ
			 * prefab[Player] �迭 �ʱ�ȭ 
			 */
			poolIndex.bar = poolIndex.note = poolIndex.noteInstance = 0;
			poolIndex.noteModel = new int[prefabs.Length];

			/**
			 * Bar Pool ����
			 * Player Clone �ڽ����� ����
			 */
			GameObject barPoolParent = new GameObject("BarPool");
			barPoolParent.transform.SetParent(players[i].transform);

			/**
			 * Bar ���� �̸� ������ ���´�.
			 * ��Ȱ��ȭ ����
			 */
			for (int j = 0; j < pool.barSize; ++j)
			{
				//Debug.Log(j + " - "+ pool.bar.Length);
				// Big, Small Bar�� ���������� ����
				pool.bar[j] = Instantiate(barPrefabs[j % 2]).GetComponent<BarInstance>();
				// Bar Instance�� BarPool �ڽ����� ��ġ
				pool.bar[j].transform.SetParent(barPoolParent.transform);
				// ��Ȱ��ȭ
				pool.bar[j].gameObject.SetActive(false);
			}

			/**
			 * Ȱ��ȭ/��Ȱ��ȭ ��Ʈ�� �ٸ� �������ִ� ����
			 */
			players[i].activeNotes = new List<List<Player.NoteInstance>>();
			for(int k=0; k<5; k++)
            {
				players[i].activeNotes.Add(new List<Player.NoteInstance>());

			}

			players[i].willRemove = new List<Player.NoteInstance>();
			players[i].activeBars = new List<BarInstance>();
			players[i].willRemoveBars = new List<BarInstance>();

			/**
			 * Player ���� output ȭ���� �����Ѵ�.
			 */
			RenderTexture output = players[i].Initialize(i, song, _playerInfos[i].difficulty, new Vector2(1024, 1024), pool, poolIndex, song.data.info.resolution, speed);
			outputs.Add(output);
		}

		/**
		 * Canvas�� �׷��ִ� render
		 * outputs ������
		 */
		sessionRenderer.Initialize(outputs.ToArray());

		/**
		 * Garbage Collect ������ ���� ��Ű��
		 */
		System.GC.Collect();
		//GcControl.GC_disable();

	}

	public void EndSession()
	{
		song = null;
		smoothing = null;
		playing = false;

		/**
		 * Pool Game Object ã�Ƽ� ����
		 */
		//foreach (Transform child in transform)
		//{
		//	if (child.name.ToLower().Contains("pool"))
		//	{
		//		Destroy(child.gameObject);
		//	}
		//}


		// Frame Index �ʱ�ȭ
		frameIndex = 0;

		/**
		 * �뷡 �ҽ��� Clip ����
		 */
		Destroy(guitarSource.clip);
		Destroy(rhythmSource.clip);
		Destroy(songSource.clip);
		Destroy(guitarSource);
		Destroy(rhythmSource);
		Destroy(songSource);
		guitarSource = rhythmSource = songSource = null;

		/**
		 * �ð� bpm tick ������ �ʱ�ȭ
		 */
		time = previousTime = 0;
		tick = 0;
		smoothTick = 0;
		starPowerDuration = 0;
		bpm = smoothBpm = 0;
		syncIndex = 0;

		/**
		 * Player �ʱ�ȭ
		 *  - Camera ��Ȱ��ȭ
		 *  - Pool ����
		 *  - Count �ʱ�ȭ
		 *  - ���� �ʱ�ȭ
		 */
		for (int i = 0; i < players.Length; ++i)
		{
			players[i].Dispose();
		}

		System.GC.Collect();
	}

	public void StartPlaying()
	{
		for (int i = 0; i < players.Length; ++i)
		{
			/**
			 * Player�� cam ȭ���� Ȱ��ȭ
			 */
			players[i].cam.gameObject.SetActive(true);
		}
		playing = true;
	}


	void Update()
	{
        /**
		 * ������ playing ���϶�
		 */
        if (songSource != null && songSource.clip != null)
        {
			// �뷡 end üũ
            if (songSource.time >= songSource.clip.length)
            {
				playing = false;
				resultScore.ShowResult();
			}

			// �뷡 �������̶�� �۾�
			if (songSource.isPlaying)
			{
				/**
				 * �ֱ�������
				 * ������� Input�� �����´�.
				 */
				for (int i = 0; i < players.Length; ++i)
				{
					players[i].GetInput();
				}

				/**
				 * frame index ����
				 */
				frameIndex++;

				/**
				 * Audio Source ���� ��� �ð� (msec)
				 */
				time = (songSource.time * 1000f);

				/**
				 * 1Frame�� ���� ��� �ɸ� �ð� (msec)
				 */
				float millisecondsPassed = time - previousTime;

				rhythmSource.time = songSource.time;
				guitarSource.time = songSource.time;

				/**
				 * ���� ����� tick�� �´� BPM���� ������Ʈ
				 * �ּ� 1���� ������Ʈ �ȴ�.
				 * �߰��� BPM�� �ٲ�� �̰����� �ٲ�� ��
				 * �����Ӹ��� ƽ�� ����
				 */
				Sync(millisecondsPassed);

				/**
				 * BPM�� �ٲ� �ε巴�� �ٲ�� ���� ��ġ
				 */
				smoothBpm = smoothing.SmoothBPM(bpm);
				/**
				 * ƽ�� ������ �ε巴�� ������ �ϱ� ���� ��ġ
				 */
				smoothTick = smoothing.SmoothTick(tick, song.data.info.resolution);

				bool playGuitarMusic = false;
				for (int i = 0; i < players.Length; ++i)
				{
					players[i].SpawnObjects(tick, beatsPerSecond);
					players[i].UpdateObjects(smoothTick, noteRenderer, frameIndex);
					players[i].CreateBar(tick);
					players[i].UpdateActiveBars(smoothTick);
					players[i].RegisterAndRemove(smoothTick);
					playGuitarMusic |= players[i].lastNoteHit;
				}
				guitarSource.volume = playGuitarMusic ? 1 : 0;

				previousTime = time;
			}
			/**
			 * ������ ���� playing ���� �ʾ�����
			 * ���� play
			 */
			else
			{
				// �뷡 ����
				if (playing)
				{
					guitarSource.Play();
					rhythmSource.Play();
					songSource.Play();
				}
			}

        }
	}

	private double beatsPerSecond, secondsPassed, beatsPassed, ticksPassed;

	private void Sync(float millisecondsPassed)
	{
		// �ʴ� ��Ʈ ��
		beatsPerSecond = bpm / 60d;

		// 1Frame�� ���� ��� �ð� (sec)
		secondsPassed = millisecondsPassed / 1000d;

		// 1Frame�� ��Ʈ ��
		beatsPassed = beatsPerSecond * secondsPassed;

		/**
		 * 1Frame�� ƽ ��
		 * resolution : 1��Ʈ�� �� ƽ�̳İ� �����ϸ� �� �� ����.
		 */
		ticksPassed = beatsPassed * song.data.info.resolution;

		// ƽ ����
		if (!double.IsNaN(ticksPassed) && bpm > 0) tick += ticksPassed;

		
		/**
		 * ���� ��ũ�� ���߱�
		 * chart ������ ����
		 * [SyncTrack] � ������ tick���� BPM�� ������ �����ؾ� �ϴ��� ���� ����
		 * ex) 0 = B 119420
		 *     768 = B 130000
		 */
		if (syncIndex < song.data.syncTrack.Count)
		{
			Song.SyncTrack nextSync = song.data.syncTrack[syncIndex];
			/**
			 * ���� tick�� ���� ��ũ ������ �ѱ�� BPM ������Ʈ
			 */
			if (nextSync.timestamp <= tick)
			{
				switch (nextSync.command)
				{
					case "B":
						bpm = nextSync.value * 0.001d;
						break;
					case "TS":
						// �� �ִ°��� �𸣰ڴ�..
						break;
				}
				syncIndex++;
			}
		}
	}

	public float TickDistanceToMeters(float tickDistance)
	{
		if (song == null) return 0;
		return (tickDistance / song.data.info.resolution) * speed;
	}
	public float MetersToTickDistance(float meters)
	{
		if (song == null) return 0;
		return (meters / speed * song.data.info.resolution);
	}
}

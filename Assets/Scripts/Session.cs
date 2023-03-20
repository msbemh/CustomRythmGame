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
	 * 노래와 난이도 초기화
	 */
	public void Initialize(Song _song, PlayerInfo[] _playerInfos)
	{
		Debug.Log("initializing ");
		song = _song;

		/**
		 * Audio Source 기타, 리듬, 노래 생성
		 */
		guitarSource = gameObject.AddComponent<AudioSource>();
		rhythmSource = gameObject.AddComponent<AudioSource>();
		songSource = gameObject.AddComponent<AudioSource>();

		// 기타, 리듬, 노래 모두 OnAwake 비활성화
		guitarSource.playOnAwake = rhythmSource.playOnAwake = songSource.playOnAwake = false;

		/**
		 * 기타, 리듬, 노래 clip 생성
		 */
		guitarSource.clip = song.audio.guitar;
		rhythmSource.clip = song.audio.rhythm;
		songSource.clip = song.audio.song;

		/**
		 * 무한 재생 끄기
		 */
		guitarSource.loop = false;
		rhythmSource.loop = false;
		songSource.loop = false;

		/**
		 * Unity 엔진에서 모든 쉐이더에서 사용할 수 있는 전역 변수를 설정
		 */
		Shader.SetGlobalFloat("_GH_Distance", RenderingFadeDistance);
		Shader.SetGlobalFloat("_GH_Fade", RenderingFadeAmount);

		/**
		 * smoothing 설정
		 */
		smoothing = new Smoothing(visualOffset);

		/**
		 * RenderTexture는 게임 오브젝트를 렌더링하여 텍스처로 만들 수 있습니다.
		 * 1. RenderTexture를 생성
		 * 2. 카메라의 targetTexture에 삽입
		 * 3. 카메라 렌더링 스타트
		 * 4. Texture2D 생성
		 * 5. RenderTexture 활성화
		 * 6. Textrue2D Read 픽셀
		 * 7. RenderTexture 비활성화
		 */
		List<RenderTexture> outputs = new List<RenderTexture>();

		// 플레이어 생성
		players = new Player[_playerInfos.Length];

		for (int i = 0; i < _playerInfos.Length; ++i)
		{
			// Player 프리팹으로 Clone 객체 생성
			players[i] = Instantiate(playerPrefab.gameObject).GetComponent<Player>();
			// Player Clone 객체의 부모를 Session으로 둔다.
			players[i].transform.SetParent(transform);
			// Player Clone 객체를 활성화
			players[i].gameObject.SetActive(true);
		}

		//XInput.SetActivePlayerCount(_playerInfos.Length);

		for (int i = 0; i < players.Length; ++i)
		{
			/**
			 * 모든 Pool을 관리해주는 객체 생성
			 * noteSize, barSize, noteInstanceSize, (사이즈)
			 * NoteModel[][] note, NoteInstance[] noteInstance, BarInstance[] bar (객체를 담는 배열)
			 * 에 대한 정보를 가진 객체
			 * 사이즈 세팅
			 */
			Player.Pool pool = new Player.Pool();
			pool.barSize = 64;
			pool.noteInstanceSize = 1024;
			pool.noteSize = 256;

			/**
			 * Pool Index 생성
			 * note, int[] noteModel, bar, noteInstance에 대한 Index 관리
			 */
			Player.PoolIndex poolIndex = new Player.PoolIndex();

			/**
			 * Note Instance 생성
			 * NoteModel noteModel (객체),
		     * timestamp, seen, star, hammeron, fred, duration에 대한 정보를 가진 객체
			 */
			pool.noteInstance = new Player.NoteInstance[pool.noteInstanceSize];
			for (int j = 0; j < pool.noteInstanceSize; ++j)
			{
				pool.noteInstance[j] = new Player.NoteInstance();
			}

			/**
			 * Note별 Pool을 생성
			 * 해당 Pool 안에는 미리 Note들을 생성해 놓는다.
			 * 실제 prefabs(빨 주 노 초 파 노트)별로 pool을 각각 생성한다
			 * 
			 * [NoteModel]
			 * Transform myTransform
			 * SpriteRenderer spriteRenderer
			 * MeshRenderer line
			 * Material materialInstance
			 * 
			 * 각 index별 Pool을 저장하게 된다.
			 */
			pool.note = new NoteModel[prefabs.Length][];
			for (int j = 0; j < prefabs.Length; ++j)
			{
				pool.note[j] = players[i].MakePool(pool.noteSize, prefabs[j]);
			}

			/**
			 * Bar Instance 생성
			 * timestamp,
			 * Transform myTransform
			 */
			pool.bar = new BarInstance[pool.barSize];

			/**
			 * pool Index를 0으로 초기화
			 * prefab[Player] 배열 초기화 
			 */
			poolIndex.bar = poolIndex.note = poolIndex.noteInstance = 0;
			poolIndex.noteModel = new int[prefabs.Length];

			/**
			 * Bar Pool 생성
			 * Player Clone 자식으로 놓기
			 */
			GameObject barPoolParent = new GameObject("BarPool");
			barPoolParent.transform.SetParent(players[i].transform);

			/**
			 * Bar 들을 미리 생성해 놓는다.
			 * 비활성화 상태
			 */
			for (int j = 0; j < pool.barSize; ++j)
			{
				//Debug.Log(j + " - "+ pool.bar.Length);
				// Big, Small Bar를 순차적으로 생성
				pool.bar[j] = Instantiate(barPrefabs[j % 2]).GetComponent<BarInstance>();
				// Bar Instance는 BarPool 자식으로 위치
				pool.bar[j].transform.SetParent(barPoolParent.transform);
				// 비활성화
				pool.bar[j].gameObject.SetActive(false);
			}

			/**
			 * 활성화/비활성화 노트와 바를 관리해주는 변수
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
			 * Player 별로 output 화면을 생성한다.
			 */
			RenderTexture output = players[i].Initialize(i, song, _playerInfos[i].difficulty, new Vector2(1024, 1024), pool, poolIndex, song.data.info.resolution, speed);
			outputs.Add(output);
		}

		/**
		 * Canvas를 그려주는 render
		 * outputs 렌더링
		 */
		sessionRenderer.Initialize(outputs.ToArray());

		/**
		 * Garbage Collect 강제로 동작 시키기
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
		 * Pool Game Object 찾아서 삭제
		 */
		//foreach (Transform child in transform)
		//{
		//	if (child.name.ToLower().Contains("pool"))
		//	{
		//		Destroy(child.gameObject);
		//	}
		//}


		// Frame Index 초기화
		frameIndex = 0;

		/**
		 * 노래 소스와 Clip 삭제
		 */
		Destroy(guitarSource.clip);
		Destroy(rhythmSource.clip);
		Destroy(songSource.clip);
		Destroy(guitarSource);
		Destroy(rhythmSource);
		Destroy(songSource);
		guitarSource = rhythmSource = songSource = null;

		/**
		 * 시간 bpm tick 정보들 초기화
		 */
		time = previousTime = 0;
		tick = 0;
		smoothTick = 0;
		starPowerDuration = 0;
		bpm = smoothBpm = 0;
		syncIndex = 0;

		/**
		 * Player 초기화
		 *  - Camera 비활성화
		 *  - Pool 삭제
		 *  - Count 초기화
		 *  - 변수 초기화
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
			 * Player의 cam 화면을 활성화
			 */
			players[i].cam.gameObject.SetActive(true);
		}
		playing = true;
	}


	void Update()
	{
        /**
		 * 음악이 playing 중일때
		 */
        if (songSource != null && songSource.clip != null)
        {
			// 노래 end 체크
            if (songSource.time >= songSource.clip.length)
            {
				playing = false;
				resultScore.ShowResult();
			}

			// 노래 진행중이라면 작업
			if (songSource.isPlaying)
			{
				/**
				 * 주기적으로
				 * 사용자의 Input을 가져온다.
				 */
				for (int i = 0; i < players.Length; ++i)
				{
					players[i].GetInput();
				}

				/**
				 * frame index 증가
				 */
				frameIndex++;

				/**
				 * Audio Source 현재 재생 시간 (msec)
				 */
				time = (songSource.time * 1000f);

				/**
				 * 1Frame당 음악 재생 걸린 시간 (msec)
				 */
				float millisecondsPassed = time - previousTime;

				rhythmSource.time = songSource.time;
				guitarSource.time = songSource.time;

				/**
				 * 현재 재생된 tick에 맞는 BPM으로 업데이트
				 * 최소 1번은 업데이트 된다.
				 * 중간에 BPM이 바뀌면 이곳에서 바뀌게 됨
				 * 프레임마다 틱이 증가
				 */
				Sync(millisecondsPassed);

				/**
				 * BPM이 바뀔때 부드럽게 바뀌게 위한 조치
				 */
				smoothBpm = smoothing.SmoothBPM(bpm);
				/**
				 * 틱이 오를때 부드럽게 오르게 하기 위한 조치
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
			 * 음악이 아직 playing 되지 않았을때
			 * 음악 play
			 */
			else
			{
				// 노래 시작
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
		// 초당 비트 수
		beatsPerSecond = bpm / 60d;

		// 1Frame당 음악 재생 시간 (sec)
		secondsPassed = millisecondsPassed / 1000d;

		// 1Frame당 비트 수
		beatsPassed = beatsPerSecond * secondsPassed;

		/**
		 * 1Frame당 틱 수
		 * resolution : 1비트당 몇 틱이냐고 생각하면 될 것 같음.
		 */
		ticksPassed = beatsPassed * song.data.info.resolution;

		// 틱 증가
		if (!double.IsNaN(ticksPassed) && bpm > 0) tick += ticksPassed;

		
		/**
		 * 음악 싱크를 맞추기
		 * chart 파일을 보면
		 * [SyncTrack] 어떤 시점의 tick에서 BPM을 몇으로 설정해야 하는지 나와 있음
		 * ex) 0 = B 119420
		 *     768 = B 130000
		 */
		if (syncIndex < song.data.syncTrack.Count)
		{
			Song.SyncTrack nextSync = song.data.syncTrack[syncIndex];
			/**
			 * 현재 tick이 음악 싱크 구간을 넘기면 BPM 업데이트
			 */
			if (nextSync.timestamp <= tick)
			{
				switch (nextSync.command)
				{
					case "B":
						bpm = nextSync.value * 0.001d;
						break;
					case "TS":
						// 왜 있는건지 모르겠다..
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

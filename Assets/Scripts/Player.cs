using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class Player : MonoBehaviour
{
	public int playerNumber;
	public PlayerInput playerInput;
	public int layerMask;
	public Song.Difficulty difficulty;
	public NoteCounter noteCounter;
	public RenderTexture output;
	public Song song;
	public List<Song.Note> notes;
	public Transform cam, board;
	public PoolIndex index;
	public Pool pool;
	public List<NoteInstance> willRemove;
	public List<List<NoteInstance>> activeNotes;

	public List<BarInstance> activeBars, willRemoveBars;
	public Line nextLine;
	public Animation2D[] flame;
	public GameObject[] fredHighlight;
	public uint resolution;
	public float speed;
	public uint nextBar;
	public bool lastNoteHit = true; //mute guitar track?


	[System.Serializable]
	public class Pool
	{
		public int noteSize;
		public int barSize;
		public int noteInstanceSize;
		public NoteModel[][] note;
		public NoteInstance[] noteInstance;
		public BarInstance[] bar;
	}

	[System.Serializable]
	public class PoolIndex
	{
		public int note;
		public int[] noteModel;
		public int bar;
		public int noteInstance;
	}

	[System.Serializable]
	public class NoteInstance
	{
		public void Update(NoteModel _noteModel, uint _timestamp, uint _fred, uint _duration, bool _star, bool _hammeron)
		{
			noteModel = _noteModel;
			timestamp = _timestamp;
			fred = _fred;
			duration = _duration;
			star = _star;
			//hammeron = _hammeron;
			hammeron = true;
			available = false;

			shortSuccess = false;
			longSuccess = false;

			fail = false;
		}
		public NoteModel noteModel;
		public uint timestamp;
		public bool seen, star, hammeron;
		public uint fred;
		public uint duration;
		public bool available;

		public bool shortSuccess;
		public bool longSuccess;
		public bool fail;
	}

	[System.Serializable]
	public class Line
	{
		public bool available;
		public int number;
		public int lowestFred;
		public double timestamp;
		public bool[] fred;
		public List<NoteInstance> note;
		public bool strumPressed, succes, fail, isHammerOn;
		public void Clear()
		{
			available = succes = fail = isHammerOn = strumPressed = false;
			timestamp = 0;
			lowestFred = 4;
			note.Clear();
			for (int i = 0; i < fred.Length; ++i)
			{
				fred[i] = false;
			}
		}
	}

	public RenderTexture Initialize(int _playerNumber, Song _song, Song.Difficulty _difficulty, Vector2 _output, Pool _pool, PoolIndex _poolIndex, uint _resolution, float _speed)
	{
		playerNumber = _playerNumber;
		// << 는 왼쪽 쉬프트 연산자
		layerMask = 1 << (10 + playerNumber);
		/**
		 * [Song.Data]
		 * Notes notes
		 * Info info
		 * List<SyncTrack> syncTrack
		 * List<SongEvent> events
		 * 
		 * [Notes]
		 * List<Song.Note> easy, medium, hard, expert;
		 * 
		 * [Note]
		 * uint timestamp, duration, fred
		 * bool star, hammerOn
		 */
		song = _song;
		switch (_difficulty)
		{
			case Song.Difficulty.Easy:
				notes = song.data.notes.easy;
				break;
			case Song.Difficulty.Medium:
				notes = song.data.notes.medium;
				break;
			case Song.Difficulty.Hard:
				notes = song.data.notes.hard;
				break;
			case Song.Difficulty.Expert:
				notes = song.data.notes.expert;
				break;
		}
		pool = _pool;
		//index = new PoolIndex();
		resolution = _resolution;
		nextBar = resolution;
		speed = _speed;
		index = _poolIndex;
		lastNoteHit = true;
		//activeNotes = new List<NoteInstance>();
		//activeBars = new List<BarInstance>();
		//willRemove = new List<NoteInstance>();
		//willRemoveBars = new List<BarInstance>();
		nextLine = new Line();
		nextLine.note = new List<NoteInstance>();
		nextLine.fred = new bool[5];

		noteCounter.Initialize();

		output = new RenderTexture(Mathf.CeilToInt(_output.x), Mathf.CeilToInt(_output.y), 16, RenderTextureFormat.ARGB32);
		cam.GetComponent<Camera>().targetTexture = output;
		/**
		 * Culling Mask는 카메라가 렌더링할 레이어를 선택하는데 사용됩니다.
		 * 레이어를 사용하여 게임 오브젝트를 분류하고, 레이어마다 다른 물리적 특성을 부여하고,
		 * 다른 카메라에서 다른 게임 오브젝트를 렌더링하도록 할 때 유용합니다.
		 */
		cam.GetComponent<Camera>().cullingMask = layerMask;

		SetLayerRecursive(transform, 10 + playerNumber);

		playerInput = new PlayerInput(PlayerInput.Device.Xinput, playerNumber);

		return output;
	}

	public void GetInput()
	{
		playerInput.Update();
	}

	public void SetLayerRecursive(Transform t, int layerMask)
	{
		foreach (Transform child in t)
		{
			//Debug.Log(child.name);
			child.gameObject.layer = layerMask;
			SetLayerRecursive(child, layerMask);
		}
	}

	public NoteModel[] MakePool(int size, GameObject prefab)
	{
		NoteModel[] newPool = new NoteModel[size];
		GameObject poolObject = new GameObject("Pool " + prefab.name);

		/**
		 * Player Clone의 자식으로 Pool Game Object가 위치하게 됨
		 */
		poolObject.transform.SetParent(transform);

		/**
		 * Pool 안에 Note Clone 생성
		 * 비활성화
		 * Note Clone은 Pool의 자식으로 위치
		 */
		for (int i = 0; i < newPool.Length; ++i)
		{

			GameObject g = Instantiate(prefab);
			g.SetActive(false);
			g.transform.SetParent(poolObject.transform);
			newPool[i] = g.GetComponent<NoteModel>();
			if (newPool[i].line != null)
			{
				newPool[i].materialInstance = newPool[i].line.material;
			}
		}

		return newPool;
	}

	public void SpawnObjects(double tick, double beatsPerSecond)
	{
		// Note 꺼낼게 없으면 return
		if (index.note >= notes.Count) return; //end of song

		// 다음 next Node
		Song.Note nextNote = notes[index.note];

		double tenSecondsInTicks = beatsPerSecond * 3 * resolution;

		/**
		 * 정확한 tick에 맞춰서
		 * 이동거리를 tick으로 환산하여 
		 * 그 시간만큼 미리 생산
		 */
		if (nextNote.timestamp < tick + MetersToTickDistance(4f))
		{
			//Debug.Log("New Note");
			try
			{
				// Long Node 인지 아닌지
				bool longNote = (nextNote.duration > 0);

				/**
				 * 어떤 pool에서 노트를 가져와야할지 판단
				 */
				int poolNumber = (int) nextNote.fred + (longNote ? 5 : 0);

				/**
				 * noteModel 가져오기
				 * 다음 index가 noteSize보다 클경우 다시 처음 인덱스부터 가져온다.
				 * 재활용한다는 뜻
				 */
				NoteModel noteModel = pool.note[poolNumber][index.noteModel[poolNumber] % pool.noteSize];

				/**
				 * noteModel을 가지고 있는 실제 노트 GameObject 가져오기
				 */
				GameObject newNote = noteModel.gameObject;
				noteModel.myTransform.rotation = cam.rotation;
				newNote.SetActive(true);

				/**
				 * NoteInstance 가져오기
				 * 이곳도 위와 같이 재활용
				 */
				NoteInstance noteInstance = pool.noteInstance[index.noteInstance % pool.noteInstanceSize];
				index.noteInstance++;

				/**
				 * NoteInstance에 대한 정보 Update
				 * 노트 인스턴스는 노트모델 정보도 가지고 있다.
				 */
				noteInstance.Update(noteModel, nextNote.timestamp, nextNote.fred, nextNote.duration, nextNote.star, nextNote.hammerOn);
				noteInstance.seen = false;

				/**
				 * fred에 맞게 List<NoteInstance> 를 넣어준다.
				 */
				List<Player.NoteInstance> list = activeNotes[System.Convert.ToInt32(noteInstance.fred)];
				list.Add(noteInstance);

				index.note++;
				index.noteModel[poolNumber]++;

				/**
				 * 1Frame에 여러개의 노트가 있을 수 있으므로 반복해서 동작
				 * 한마디로 동시에 여러개 나올때
				 */
				SpawnObjects(tick, beatsPerSecond);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message + " - " + e.StackTrace);
			}
		}
	}

	public void Dispose()
	{
		song = null;
		foreach (Transform child in transform)
		{
			if (child.name.ToLower().Contains("pool"))
			{
				Destroy(child.gameObject);
			}
		}
		cam.gameObject.SetActive(false);
		pool = null;
		index = null;
		Destroy(gameObject);
	}

	public void UpdateObjects(double smoothTick, NoteRenderer noteRenderer, int frameIndex)
	{
		/**
		 * Board를 이동 시킨다.
		 */
		Vector3 boardPosition = board.localPosition;
		boardPosition.z = (float)((TickDistanceToMeters(smoothTick) % 2) * -1f + 4);
		if (!float.IsNaN(boardPosition.z))
		{
			board.localPosition = boardPosition;
		}

        /**
		 * 노트를 이동 시킨다.
		 */
		for(int k=0; k<activeNotes.Count; k++)
        {
			for (int i = 0; i < activeNotes[k].Count; ++i)
			{
				NoteInstance noteInstance = activeNotes[k][i];
				Transform noteTransform = noteInstance.noteModel.transform;
				Vector3 pos = noteTransform.localPosition;

				/**
				 * 실제 도착해야할 tick 시간과 현재 tick시간을 계산하여
				 * 거리 계산
				 */
				double tickDistance = noteInstance.timestamp - smoothTick;
				double distanceInMeters = TickDistanceToMeters(tickDistance);

				/**
				 * 계산된 거리만큼 노트 이동
				 */
				pos.z = (float)distanceInMeters;
				noteTransform.localPosition = pos;


				/**
				 * 긴 노트일 경우 길이를 계산하여 세팅
				 */
				double noteDistance = tickDistance;
				double noteDistanceInMeters = TickDistanceToMeters(noteDistance);
				double endOfNoteDistance = tickDistance + noteInstance.duration;
				double endOfNoteInMeters = TickDistanceToMeters(endOfNoteDistance);
				if (noteInstance.duration > 0)
				{
					//update long note length
					float length = (float)(endOfNoteInMeters - distanceInMeters);
					noteInstance.noteModel.SetLengt(length);
				}

				/**
				 * Hammer, Star, Normal에 따른 모양 변형
				 */
				SpriteRenderer spriteRenderer = noteInstance.noteModel.spriteRenderer;
				NoteRenderer.FredSpriteData fredSpriteData = noteRenderer.spriteData.fred[noteInstance.fred];
				if (noteInstance.star)
				{
					spriteRenderer.sprite = (noteInstance.hammeron) ? fredSpriteData.starHammerOn[frameIndex % 16] : fredSpriteData.star[frameIndex % 16];
				}
				else
				{
					spriteRenderer.sprite = (noteInstance.hammeron) ? fredSpriteData.hammerOn : fredSpriteData.normal;
				}

				if (endOfNoteInMeters < -1) //out of view
				{
					willRemove.Add(noteInstance);
				}
			}
		}
	}

	public void CreateBar(double tick)
	{
		/**
		 * 정확한 tick에 bar를 생성
		 * Bar생성 지점부터 판정선까지의 거리(4)를 시간으로 계산해서 
		 * 그 시간만큼 미리 생성
		 */
		if (nextBar < tick + MetersToTickDistance(4f))
		{
			BarInstance newBar = pool.bar[index.bar % pool.barSize];
			index.bar++;
			newBar.gameObject.SetActive(true);
			newBar.timestamp = nextBar;
			activeBars.Add(newBar);
			nextBar += resolution;
		}
	}

	public void UpdateActiveBars(double smoothTick)
	{
		for (int i = 0; i < activeBars.Count; ++i)
		{
			BarInstance barInstance = activeBars[i];
			/**
			 * 현재 tick과 Bar가 판정선에 도달할때의 tick의 차이
			 */
			double tickDistance = barInstance.timestamp - smoothTick;

			/**
			 * tick 차이를 거리로 환산
			 */
			double distanceInMeters = TickDistanceToMeters(tickDistance);

			/**
			 * Bar의 위치를 부드럽게 이동시킴
			 */
			Vector3 pos = barInstance.myTransform.localPosition;
			pos.z = (float) distanceInMeters;
			barInstance.myTransform.localPosition = pos;

			/**
			 * 판정선을 넘은 Bar는 삭제
			 */
			if (tickDistance < 0)
			{
				willRemoveBars.Add(barInstance);
			}
		}

		/**
		 * Bar 삭제
		 */
		for (int i = willRemoveBars.Count - 1; i > -1; --i)
		{
			activeBars.Remove(willRemoveBars[i]);
			willRemoveBars[i].gameObject.SetActive(false);
			willRemoveBars.RemoveAt(i);
		}
	}

	/**
	 * 매 프레임마다
	 * 노트 이용 가능여부 및 사용자 입력 검사
	 * 기준선에 맞게 입력하면 제거
	 */
	public void RegisterAndRemove(double smoothTick)
	{
		/**
		 * HightLight 표시
		 */
		for(int i=0; i<playerInput.fredHighlight.Length; i++)
        {
			fredHighlight[i].SetActive(playerInput.fredHighlight[i]);
		}

		double window = resolution / 4;

		/**
		 * 각 Fred별 ActiveNotes의 첫번째 NoteInstance가 판단선에 접근해서
		 * 이용가능한지 여부를 조사한다.
		 * 
		 */
		for(int k=0; k<activeNotes.Count; k++)
        {
			List<Player.NoteInstance> fredActiveNotes = activeNotes[k];
			if (fredActiveNotes.Count > 0 && (fredActiveNotes[0].timestamp <= (smoothTick + (window * 1.5))))
			{
				NoteInstance noteInstance = fredActiveNotes[0];
				noteInstance.available = true;
			}
		}

		/**
		 * 각 Fred별 ActiveNotes의 첫번째 NoteInstance가
		 * 이용 가능하다면 사용자 입력이 눌렸는지 조사한다.
		 */
		for (int k = 0; k < activeNotes.Count; k++)
		{
			List<Player.NoteInstance> fredActiveNotes = activeNotes[k];

			if(fredActiveNotes.Count > 0)
            {
				NoteInstance noteInstance = fredActiveNotes[0];

				if (noteInstance.available)
				{
					uint fred = noteInstance.fred;

					/**
					 * 성공 여부 판단 하는 곳
					 */
					// Short 노트
					if (noteInstance.duration < 1)
					{
						// 성공일 때
						if (playerInput.fred[fred]) noteInstance.shortSuccess = true;
					}
					// Long 노트
					else
					{
						// 진입 성공
						if (playerInput.fred[fred] || !noteInstance.fail) { 
							noteInstance.longSuccess = true;
						}

						if (noteInstance.longSuccess)
                        {
                            // 계속 꾹 버튼을 누르고 있지 않다면 실패 
                            if (!playerInput.fredHighlight[fred])
                            {
								noteInstance.longSuccess = false;
							}
                        }
					}

					/**
					 * 시간 초과 되면 실패
					 */
					if ((noteInstance.timestamp - smoothTick) < -window)
					{
						noteInstance.fail = true;
					}

					/**
					 * 노트 시간초과로 실패했을 때
					 */
					if (noteInstance.fail)
					{
						// Short만 삭제
						if(noteInstance.duration == 0)
                        {
							willRemove.Add(noteInstance);
						}
						lastNoteHit = false;
					}

					/**
					 * Short 성공 후처리
					 */
					if (noteInstance.shortSuccess && !noteInstance.fail)
					{
						//Debug.Log("HIT");
						/**
						 * 노트삭제
						 * 맞췄을떄 Effect 처리
						 */
						noteInstance.shortSuccess = false;
						willRemove.Add(noteInstance);

						uint fred2 = noteInstance.fred;
						flame[fred2].gameObject.SetActive(true);
						flame[fred2].Reset();
						flame[fred2].seconds = (1f / 60f * 8f);
						noteCounter.number++;
						lastNoteHit = true;
					}

					/**
					 * Long 성공 후처리
					 */
					if (noteInstance.longSuccess)
					{
						//Debug.Log("HIT");
						//willRemove.Add(noteInstance);
						//double distanceInMeters = TickDistanceToMeters(smoothTick);
						//if ((distanceInMeters % 1) == 0)
						//{
						//	Debug.Log("들어옴");
						//}

						uint fred2 = noteInstance.fred;
						flame[fred2].gameObject.SetActive(true);
						flame[fred2].Reset();
						flame[fred2].seconds = (1f / 60f * 8f);
						noteCounter.number++;
						lastNoteHit = true;
					}

                    /**
					 * Long 꼬리 길이 전부 지나면 삭제
					 */
                    if (noteInstance.duration > 0)
                    {
						if (smoothTick >= noteInstance.timestamp + noteInstance.duration)
						{
							noteInstance.longSuccess = false;
							willRemove.Add(noteInstance);
						}
					}
				}
			}
			
		}

		/**
		 * 제거 활동
		 */
		for (int i = willRemove.Count - 1; i > -1; --i)
		{
			/**
			 * 활성 노트 리스트에서 제거
			 */
			for (int k = 0; k < activeNotes.Count; k++)
			{
				List<Player.NoteInstance> list = activeNotes[k];
				list.Remove(willRemove[i]);

			}

			/**
			 * 헤당 모델 자체 비활성화 설정
			 */
			willRemove[i].noteModel.transform.gameObject.SetActive(false);

			/**
			 * WillRemove 리스트에서 제거
			 */
			willRemove.RemoveAt(i);
		}

		noteCounter.UpdateCounter();
	}

	public double TickDistanceToMeters(double tickDistance)
	{
		return (tickDistance / resolution) * speed;
	}

	private double MetersToTickDistance(double meters)
	{
		return (meters / speed * resolution);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	public List<NoteInstance> activeNotes, willRemove;
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
		}
		public NoteModel noteModel;
		public uint timestamp;
		public bool seen, star, hammeron;
		public uint fred;
		public uint duration;
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
				activeNotes.Add(noteInstance);

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
		for (int i = 0; i < activeNotes.Count; ++i)
		{
			NoteInstance noteInstance = activeNotes[i];
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
			pos.z = (float) distanceInMeters;
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

	public void RegisterAndRemoveCustom(double smoothTick)
	{

		
	}
	/**
	 * 매 프레임 마다 들어옴
	 */
	public void RegisterAndRemove(double smoothTick)
	{
		bool missedThisFrame = false;

		//highlighting player input
		//for (int i = 0; i < playerInput.fred.Length; ++i)
		//{
		//	fredHighlight[i].SetActive(playerInput.fred[i]);
		//}

		/**
		 * HightLight 표시
		 */
		for(int i=0; i<playerInput.fredHighlight.Length; i++)
        {
			fredHighlight[i].SetActive(playerInput.fredHighlight[i]);
		}

		double window = resolution / 4;

		/**
		 * 가장 앞선 노트에 대해서 키보드를 press 했을 때 반응할 수 있는지에 대한 여부
		 */
		if (!nextLine.available)
		{
			//check if strum bar is hit while there is no new line. this breaks combo
			if (playerInput.strumPressed)
			{
				//noteCounter.number = 0;
				//Debug.Log("Strummed without a new line being available");
			}

			/**
			 * 타이밍 맞게 nextLine에 해당 노트를 넣어 둔다.
			 */
			if (activeNotes.Count > 0 && (activeNotes[0].timestamp <= (smoothTick + (window * 0.5))))
			{
				// nextLine에 노트 추가
				nextLine.note.Add(activeNotes[0]);
				nextLine.timestamp = activeNotes[0].timestamp;
				nextLine.isHammerOn = activeNotes[0].hammeron;

				//Debug.Log("Creating new line with timestamp "+nextLine.timestamp);

				int i = 1;
				/**
				 * 같은 timestamp인 노트들은 더 추가한다.
				 */
				while (i < 5)
				{
					if (i >= activeNotes.Count)
					{
						//Debug.Log("No more active notes");
						break; //out of range
					}
					if (activeNotes[i].timestamp != nextLine.timestamp)
					{
						//Debug.Log("active note "+i+" has a different timestamp of "+activeNotes[i].timestamp);
						break; //different line
					}
					//Debug.Log("Adding one more note");
					nextLine.note.Add(activeNotes[i]);
					i++;
				}

				/**
				 * nextLine에 어떤 fred를 칠 수 있는 상태인지 표시
				 * 가장 낮은 fred 구하기
				 */
				nextLine.lowestFred = 5;
				for (int j = 0; j < nextLine.note.Count; ++j)
				{
					uint fred = nextLine.note[j].fred;
					nextLine.lowestFred = Mathf.Min(nextLine.lowestFred, (int)fred);
					nextLine.fred[fred] = true;
				}
				nextLine.available = true;
				//string debugNotes = "";
				//for (int j = 0; j < nextLine.note.Count; ++j)
				//{
				//	debugNotes += nextLine.note[j].fred.ToString() + " ";
				//}
				//Debug.Log("Creating new line with notes "+ debugNotes);
			}
			else
			{
				//Debug.Log("No New Notes");
			}
		}

		/**
		 * 노트를 칠 수 있을 때
		 */
		if (nextLine.available)
		{
			/**
			 * 동시에 눌러야 되는 상황까지 포함하여 정확하게 눌렀는지 판단
			 * 그런데 이부분은 가장 낮은 Fred부터 나머지 부분이 정확한지 판단함..
			 * 그래서 Fred를 전부 동시에 누르고 있으면 마지막 부분의 노트만 내려올때 성공이됨...
			 */
			// 정확하게 눌렀는지 판단하기 위한 변수
			//bool correctColors = true;
			//for (int i = nextLine.lowestFred; i < playerInput.fred.Length; ++i)
			//{
			//	//Debug.Log("fred "+i+" "+playerInput.fred[i] + " needs to equal " + nextLine.fred[i]);
			//	correctColors &= (playerInput.fred[i] == nextLine.fred[i]);
			//}
			bool correctColors = true;
			for (int i=0; i < playerInput.fred.Length; ++i)
			{
				//Debug.Log("fred "+i+" "+playerInput.fred[i] + " needs to equal " + nextLine.fred[i]);
				correctColors &= (playerInput.fred[i] == nextLine.fred[i]);
			}

			/**
			 * 그래서 이렇게 변경함
			 */
			//bool correctColors = true;
			//for (int i=0; i<nextLine.note.Count; i++)
			//{
			//	NoteInstance noteInstance = nextLine.note[i];
			//	uint fred = noteInstance.fred;
			//    if (nextLine.fred[fred] != playerInput.fred[fred])
			//    {
			//		correctColors = false;
			//
			//	}
			//	//correctColors &= (playerInput.fred[i] == nextLine.note[i].fred);
			//}



			//Debug.Log("Holding correct colors " + correctColors);

			//Check if strum has already been pressed, 
			//if the colors are pressed on time afterwards it will register and exit here
			//also check if hammerOn, then no strum will be necessary

			// if ((nextLine.strumPressed || nextLine.isHammerOn) && correctColors)
			// 아래 처럼 바꿈
			if (correctColors)
			{
				nextLine.succes = true;
				//Debug.Log("Pressed strum after holding correct colors");
			}

			/**
			 * strumPress 기능은 뺐기 때문에
			 * 주석처리
			 */
			//else
			//{
			//	//check for strum input
			//	if (playerInput.strumPressed)
			//	{
			//		//Debug.Log("Strum Pressed");
			//		//check if inside window
			//		if (Mathf.Abs((float)(nextLine.timestamp - smoothTick)) <= window)
			//		{
			//			//Debug.Log("Inside of window! correct colors yet: " + correctColors);
			//			//check if double strum pressed, this is a fail
			//			if (nextLine.strumPressed) nextLine.fail = true;
			//			nextLine.strumPressed = true;
			//			if (correctColors && !nextLine.fail) nextLine.succes = true;
			//		}
			//		else
			//		{
			//			//strummed too early
			//			//Debug.Log("Strummed too early");
			//			//noteCounter.number = 0;
			//		}
			//	}
			//	else
			//	{
			//		//Debug.Log("Strum not pressed");
			//	}
			//}

			/**
			 * 해당 nextLine이 넘어갔을 경우 fail처리
			 */
			if ((nextLine.timestamp - smoothTick) < -window)
			{
				nextLine.fail = true;
				//Debug.Log("Too late. note: " + nextLine.timestamp + ". strum: " + smoothTick);
				//Redo this function again when too late to see if the next set of notes is hit
				//RegisterHits(smoothTick);
			}

			/**
			 * nextLine이 fail일 경우
			 */
			if (nextLine.fail)
			{
				//Debug.Log("MISS");

				/**
				 * nextLine에 있는 노트 제거
				 */
				for (int i = 0; i < nextLine.note.Count; ++i)
				{
					willRemove.Add(nextLine.note[i]);
				}

				/**
				 * nextLine 클리어
				 */
				nextLine.Clear();

				//noteCounter.number = 0;

				lastNoteHit = false;
				missedThisFrame = true;
			}

			/**
			 * 성공했을 경우
			 */
			if (nextLine.succes && !nextLine.fail)
			{
				//Debug.Log("HIT");
				/**
				 * nextLine 노트 삭제
				 * 맞췄을떄 Effect 처리
				 */
				for (int i = 0; i < nextLine.note.Count; ++i)
				{
					willRemove.Add(nextLine.note[i]);
					uint fred = nextLine.note[i].fred;
					flame[fred].gameObject.SetActive(true);
					flame[fred].Reset();
					flame[fred].seconds = (1f / 60f * 8f);
				}
				nextLine.Clear();
				noteCounter.number++;
				lastNoteHit = true;
			}
		}

		/**
		 * 활성 노트에서 해당 노트를 삭제
		 * willRemove에 있는 모든 노트 삭제
		 * noteModel 비활성화
		 */
		for (int i = willRemove.Count - 1; i > -1; --i)
		{
			activeNotes.Remove(willRemove[i]);
			willRemove[i].noteModel.transform.gameObject.SetActive(false);
			willRemove.RemoveAt(i);
		}

		//update note counter
		//noteCounter.gameObject.SetActive(noteCounter.number > 30);
		noteCounter.UpdateCounter();

		//if missed a note, do function again to check if next note is hit instead. but break combo
		if (missedThisFrame) RegisterAndRemove(smoothTick);
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

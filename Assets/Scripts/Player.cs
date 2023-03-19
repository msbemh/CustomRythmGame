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
		// << �� ���� ����Ʈ ������
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
		 * Culling Mask�� ī�޶� �������� ���̾ �����ϴµ� ���˴ϴ�.
		 * ���̾ ����Ͽ� ���� ������Ʈ�� �з��ϰ�, ���̾�� �ٸ� ������ Ư���� �ο��ϰ�,
		 * �ٸ� ī�޶󿡼� �ٸ� ���� ������Ʈ�� �������ϵ��� �� �� �����մϴ�.
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
		 * Player Clone�� �ڽ����� Pool Game Object�� ��ġ�ϰ� ��
		 */
		poolObject.transform.SetParent(transform);

		/**
		 * Pool �ȿ� Note Clone ����
		 * ��Ȱ��ȭ
		 * Note Clone�� Pool�� �ڽ����� ��ġ
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
		// Note ������ ������ return
		if (index.note >= notes.Count) return; //end of song

		// ���� next Node
		Song.Note nextNote = notes[index.note];

		double tenSecondsInTicks = beatsPerSecond * 3 * resolution;

		/**
		 * ��Ȯ�� tick�� ���缭
		 * �̵��Ÿ��� tick���� ȯ���Ͽ� 
		 * �� �ð���ŭ �̸� ����
		 */
		if (nextNote.timestamp < tick + MetersToTickDistance(4f))
		{
			//Debug.Log("New Note");
			try
			{
				// Long Node ���� �ƴ���
				bool longNote = (nextNote.duration > 0);

				/**
				 * � pool���� ��Ʈ�� �����;����� �Ǵ�
				 */
				int poolNumber = (int) nextNote.fred + (longNote ? 5 : 0);

				/**
				 * noteModel ��������
				 * ���� index�� noteSize���� Ŭ��� �ٽ� ó�� �ε������� �����´�.
				 * ��Ȱ���Ѵٴ� ��
				 */
				NoteModel noteModel = pool.note[poolNumber][index.noteModel[poolNumber] % pool.noteSize];

				/**
				 * noteModel�� ������ �ִ� ���� ��Ʈ GameObject ��������
				 */
				GameObject newNote = noteModel.gameObject;
				noteModel.myTransform.rotation = cam.rotation;
				newNote.SetActive(true);

				/**
				 * NoteInstance ��������
				 * �̰��� ���� ���� ��Ȱ��
				 */
				NoteInstance noteInstance = pool.noteInstance[index.noteInstance % pool.noteInstanceSize];
				index.noteInstance++;

				/**
				 * NoteInstance�� ���� ���� Update
				 * ��Ʈ �ν��Ͻ��� ��Ʈ�� ������ ������ �ִ�.
				 */
				noteInstance.Update(noteModel, nextNote.timestamp, nextNote.fred, nextNote.duration, nextNote.star, nextNote.hammerOn);
				noteInstance.seen = false;
				activeNotes.Add(noteInstance);

				index.note++;
				index.noteModel[poolNumber]++;

				/**
				 * 1Frame�� �������� ��Ʈ�� ���� �� �����Ƿ� �ݺ��ؼ� ����
				 * �Ѹ���� ���ÿ� ������ ���ö�
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
		 * Board�� �̵� ��Ų��.
		 */
		Vector3 boardPosition = board.localPosition;
		boardPosition.z = (float)((TickDistanceToMeters(smoothTick) % 2) * -1f + 4);
		if (!float.IsNaN(boardPosition.z))
		{
			board.localPosition = boardPosition;
		}

		/**
		 * ��Ʈ�� �̵� ��Ų��.
		 */
		for (int i = 0; i < activeNotes.Count; ++i)
		{
			NoteInstance noteInstance = activeNotes[i];
			Transform noteTransform = noteInstance.noteModel.transform;
			Vector3 pos = noteTransform.localPosition;

			/**
			 * ���� �����ؾ��� tick �ð��� ���� tick�ð��� ����Ͽ�
			 * �Ÿ� ���
			 */
			double tickDistance = noteInstance.timestamp - smoothTick;
			double distanceInMeters = TickDistanceToMeters(tickDistance);

			/**
			 * ���� �Ÿ���ŭ ��Ʈ �̵�
			 */
			pos.z = (float) distanceInMeters;
			noteTransform.localPosition = pos;


			/**
			 * �� ��Ʈ�� ��� ���̸� ����Ͽ� ����
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
			 * Hammer, Star, Normal�� ���� ��� ����
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
		 * ��Ȯ�� tick�� bar�� ����
		 * Bar���� �������� ������������ �Ÿ�(4)�� �ð����� ����ؼ� 
		 * �� �ð���ŭ �̸� ����
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
			 * ���� tick�� Bar�� �������� �����Ҷ��� tick�� ����
			 */
			double tickDistance = barInstance.timestamp - smoothTick;

			/**
			 * tick ���̸� �Ÿ��� ȯ��
			 */
			double distanceInMeters = TickDistanceToMeters(tickDistance);

			/**
			 * Bar�� ��ġ�� �ε巴�� �̵���Ŵ
			 */
			Vector3 pos = barInstance.myTransform.localPosition;
			pos.z = (float) distanceInMeters;
			barInstance.myTransform.localPosition = pos;

			/**
			 * �������� ���� Bar�� ����
			 */
			if (tickDistance < 0)
			{
				willRemoveBars.Add(barInstance);
			}
		}

		/**
		 * Bar ����
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
	 * �� ������ ���� ����
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
		 * HightLight ǥ��
		 */
		for(int i=0; i<playerInput.fredHighlight.Length; i++)
        {
			fredHighlight[i].SetActive(playerInput.fredHighlight[i]);
		}

		double window = resolution / 4;

		/**
		 * ���� �ռ� ��Ʈ�� ���ؼ� Ű���带 press ���� �� ������ �� �ִ����� ���� ����
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
			 * Ÿ�̹� �°� nextLine�� �ش� ��Ʈ�� �־� �д�.
			 */
			if (activeNotes.Count > 0 && (activeNotes[0].timestamp <= (smoothTick + (window * 0.5))))
			{
				// nextLine�� ��Ʈ �߰�
				nextLine.note.Add(activeNotes[0]);
				nextLine.timestamp = activeNotes[0].timestamp;
				nextLine.isHammerOn = activeNotes[0].hammeron;

				//Debug.Log("Creating new line with timestamp "+nextLine.timestamp);

				int i = 1;
				/**
				 * ���� timestamp�� ��Ʈ���� �� �߰��Ѵ�.
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
				 * nextLine�� � fred�� ĥ �� �ִ� �������� ǥ��
				 * ���� ���� fred ���ϱ�
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
		 * ��Ʈ�� ĥ �� ���� ��
		 */
		if (nextLine.available)
		{
			/**
			 * ���ÿ� ������ �Ǵ� ��Ȳ���� �����Ͽ� ��Ȯ�ϰ� �������� �Ǵ�
			 * �׷��� �̺κ��� ���� ���� Fred���� ������ �κ��� ��Ȯ���� �Ǵ���..
			 * �׷��� Fred�� ���� ���ÿ� ������ ������ ������ �κ��� ��Ʈ�� �����ö� �����̵�...
			 */
			// ��Ȯ�ϰ� �������� �Ǵ��ϱ� ���� ����
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
			 * �׷��� �̷��� ������
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
			// �Ʒ� ó�� �ٲ�
			if (correctColors)
			{
				nextLine.succes = true;
				//Debug.Log("Pressed strum after holding correct colors");
			}

			/**
			 * strumPress ����� ���� ������
			 * �ּ�ó��
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
			 * �ش� nextLine�� �Ѿ�� ��� failó��
			 */
			if ((nextLine.timestamp - smoothTick) < -window)
			{
				nextLine.fail = true;
				//Debug.Log("Too late. note: " + nextLine.timestamp + ". strum: " + smoothTick);
				//Redo this function again when too late to see if the next set of notes is hit
				//RegisterHits(smoothTick);
			}

			/**
			 * nextLine�� fail�� ���
			 */
			if (nextLine.fail)
			{
				//Debug.Log("MISS");

				/**
				 * nextLine�� �ִ� ��Ʈ ����
				 */
				for (int i = 0; i < nextLine.note.Count; ++i)
				{
					willRemove.Add(nextLine.note[i]);
				}

				/**
				 * nextLine Ŭ����
				 */
				nextLine.Clear();

				//noteCounter.number = 0;

				lastNoteHit = false;
				missedThisFrame = true;
			}

			/**
			 * �������� ���
			 */
			if (nextLine.succes && !nextLine.fail)
			{
				//Debug.Log("HIT");
				/**
				 * nextLine ��Ʈ ����
				 * �������� Effect ó��
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
		 * Ȱ�� ��Ʈ���� �ش� ��Ʈ�� ����
		 * willRemove�� �ִ� ��� ��Ʈ ����
		 * noteModel ��Ȱ��ȭ
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

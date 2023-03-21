using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class SongBlock : MonoBehaviour
{
	public FileInfo fileInfo;
	public SongSelect songSelect;
	public Text text;
	public void Play()
	{
		Debug.Log("Play 시작");
		songSelect.LoadSong(fileInfo);

		// 다시시작을 위해서 저장
		Session.songBlock = this;
	}
}
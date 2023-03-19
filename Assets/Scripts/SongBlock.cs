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
		Debug.Log("Play Ω√¿€");
		songSelect.LoadSong(fileInfo);
	}
}
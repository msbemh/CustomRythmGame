using UnityEngine;
using System.Collections;

[System.Serializable]
public class PlayerInput
{
	public int deviceId;
	public Device device;
	public bool[] fred;
	public bool[] fredHighlight;
	public bool strumPressed;
	public bool startPressed;
	public bool starPressed;
	public float tilt, whammy;

	public enum Device
	{
		Keyboard,
		Xinput
	}
	public PlayerInput(Device _device, int _deviceId)
	{
		device = _device;
		deviceId = _deviceId;
		fred = new bool[5];
		fredHighlight = new bool[5];
	}
	public void Update()
	{
		//fred[0] = XInput.GetButton(deviceId, XInput.Button.A);
		//fred[1] = XInput.GetButton(deviceId, XInput.Button.B);
		//fred[2] = XInput.GetButton(deviceId, XInput.Button.Y);
		//fred[3] = XInput.GetButton(deviceId, XInput.Button.X);
		//fred[4] = XInput.GetButton(deviceId, XInput.Button.LB);
		//startPressed = XInput.GetButtonDown(deviceId, XInput.Button.Start);

		fred[0] = Input.GetKey(KeyCode.D);
		fred[1] = Input.GetKey(KeyCode.F);
		fred[2] = Input.GetKey(KeyCode.Space);
		fred[3] = Input.GetKey(KeyCode.J);
		fred[4] = Input.GetKey(KeyCode.K);

		fredHighlight[0] = Input.GetKey(KeyCode.D);
		fredHighlight[1] = Input.GetKey(KeyCode.F);
		fredHighlight[2] = Input.GetKey(KeyCode.Space);
		fredHighlight[3] = Input.GetKey(KeyCode.J);
		fredHighlight[4] = Input.GetKey(KeyCode.K);

		//if (fred[0]) Debug.Log("frd[0]:" + fred[0]);
		//if (fred[1]) Debug.Log("frd[1]:" + fred[1]);
		//if (fred[2]) Debug.Log("frd[2]:" + fred[2]);
		//if (fred[3]) Debug.Log("frd[3]:" + fred[3]);
		//if (fred[4]) Debug.Log("frd[4]:" + fred[4]);

		//if (fred[0]) Debug.Log("frd[0]:" + fred[0]);
		//if (fred[1]) Debug.Log("frd[1]:" + fred[1]);
		//if (fred[2]) Debug.Log("frd[2]:" + fred[2]);
		//if (fred[3]) Debug.Log("frd[3]:" + fred[3]);
		//if (fred[4]) Debug.Log("frd[4]:" + fred[4]);

		strumPressed = Input.GetKey(KeyCode.Z);
		strumPressed = Input.GetKey(KeyCode.X);
		//tilt = Input.GetKey(KeyCode.C);
		//whammy = Input.GetKey(KeyCode.V);
		//starPressed = XInput.GetButtonDown(deviceId, XInput.Button.Back);
		//strumPressed = XInput.GetButtonDown(deviceId, XInput.Button.DPadDown) | XInput.GetButtonDown(deviceId, XInput.Button.DPadUp);
		//tilt = XInput.GetAxis(deviceId, XInput.Axis.RY);
		//whammy = XInput.GetAxis(deviceId, XInput.Axis.RX);
	}
}


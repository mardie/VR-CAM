using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using System;
using System.IO;
using System.Text;

public class Main : MonoBehaviour
{
    SerialPort stream = new SerialPort("COM3", 115200);
    float[] q = new float[4];
	float focalLength = 50;
	string warnings = "";
	Boolean takingPhoto = false;
    

    void Start()
    {
        stream.ReadTimeout = 2000;
	//This encoding let us use the 8 bits per byte
        stream.Encoding = Encoding.GetEncoding(28591);

        stream.Open(); 
        stream.Write("r");
    }

    void Shutter()
    {
		GameObject shutter = GameObject.Find ("Shutter");
		shutter.transform.localScale = new Vector3 (0, 0, 0);
		Invoke("ResetTakingPhoto", 0.8f);
    }

	void ResetTakingPhoto(){
		takingPhoto = false;
	}

	void ResetWarnings(){
		warnings = "";
	}

	void takePicture(){
		takingPhoto = true;
		GameObject a = GameObject.Find("Shutter");
		a.transform.localScale = new Vector3 (1, 1, 1);
		Invoke("Shutter", 0.1f);
		scorePicture ();
		Invoke("ResetWarnings", 5f);

	}

	void scorePicture(){
		warnings = "";
		if (Camera.main.transform.rotation.z < -0.1 || Camera.main.transform.rotation.z > 0.1) {
			warnings += "Camera is tilted.\n";
		}

		if (Camera.main.transform.rotation.y > 0.55 && Camera.main.transform.rotation.y < 0.80) {
			warnings += "Picture at backlit. Look at the shadows.";
		}
	}

	float getAngleFromFocalLength(float focal){
		float radians = (float)(2 * Math.Atan ((0.5 * 36) / focal));

		return (float)(radians * (180 / Math.PI));
	}

    void Update()
    {
		try{
	        string input = stream.ReadLine();
			byte[] dataPacket = new byte[15];
			stream.Read(dataPacket, 0, 15);

			if (dataPacket.Length != 15 || (char)dataPacket[0] != '$')
	        {
	            return;
	        }

			Boolean button1 = (dataPacket[10] & 1) == 1;
			Boolean button2 = (dataPacket[10] & 2) == 2;
			Boolean button3 = (dataPacket[10] & 4) == 4;
			Boolean button4 = (dataPacket[10] & 8) == 8;

			if (button1 && focalLength < 300) {
				focalLength++;
			}
				
			if (button2 && focalLength > 28) {
				focalLength--;
			}


			if (button4 && !takingPhoto) {
				takePicture ();
			}

			Camera.main.fieldOfView = getAngleFromFocalLength (focalLength);

	        q[0] = (((byte)dataPacket[2] << 8) | (byte)dataPacket[3]) / 16384.0f;
	        q[1] = (((byte)dataPacket[4] << 8) | (byte)dataPacket[5]) / 16384.0f;
	        q[2] = (((byte)dataPacket[6] << 8) | (byte)dataPacket[7]) / 16384.0f;
	        q[3] = (((byte)dataPacket[8] << 8) | (byte)dataPacket[9]) / 16384.0f;
	        for (int i = 0; i < 4; i++) if (q[i] >= 2f) q[i] = -4 + q[i];

			if(q[0]== 0 || q[1] == 0 || q[2] == 0 || q[3] == 0)
	        {
	            return;
	        }
	    
	        Camera.main.transform.rotation = new Quaternion(-q[1], -q[3], -q[2], q[0]);

	        stream.BaseStream.Flush();
		}catch(Exception TimoutException){
		}
    }


    void OnGUI()
    {
		GUI.skin.label.fontSize = 300;

		GUIStyle warningStyle = new GUIStyle();
		warningStyle.fontSize = 30;
		warningStyle.normal.textColor = Color.red;

		GUIStyle defaultStyle = new GUIStyle();
		defaultStyle.fontSize = 20;
		defaultStyle.normal.textColor = Color.black;
	
		GUI.Label(new Rect(10, 10, 300, 100), warnings, warningStyle);
		
		string cameraSettings = "Focal length:" + ((int)focalLength) +  "   " + Camera.main.transform.rotation.x.ToString() + "   " + Camera.main.transform.rotation.y.ToString()+ " " + Camera.main.transform.rotation.z.ToString();
		GUI.Label (new Rect (10, Screen.height - defaultStyle.fontSize - 10, 300, 100), cameraSettings, defaultStyle);

	}
}

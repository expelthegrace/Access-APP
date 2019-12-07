using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using QRCodeReaderAndGenerator;

public class QRCodeReaderExample : MonoBehaviour {

	[SerializeField]
	RawImage rawImage;

	[SerializeField]
	Text txtResult;

	// Use this for initialization

	IEnumerator Start()
	{
		yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
	}


	void OnEnable () {
		QRCodeManager.onError += HandleOnError;
		QRCodeManager.onQrCodeFound += HandleOnQRCodeFound;
	}

	void OnDisable () {
		QRCodeManager.onError -= HandleOnError;
		QRCodeManager.onQrCodeFound -= HandleOnQRCodeFound;
	}

	void HandleOnQRCodeFound (ZXing.BarcodeFormat barCodeType, string barCodeValue)
	{
		Debug.Log (barCodeType + " __ " + barCodeValue);
		txtResult.text = barCodeValue;
	}

	void HandleOnError (string err)
	{
		Debug.LogError (err);
	}
		
	public void ScanQRCode()
	{
		if(rawImage)
		{
			QRCodeManager.CameraSettings camSettings = new QRCodeManager.CameraSettings ();
			string rearCamName = GetRearCamName ();
			if (rearCamName != null) {
				camSettings.deviceName = rearCamName;
				camSettings.maintainAspectRatio = true;
				camSettings.makeSquare = true;
				camSettings.requestedWidth = 1280;
				camSettings.requestedHeight = 720;
				camSettings.scanType = ScanType.CONTINUOUS;
				QRCodeManager.Instance.ScanQRCode (camSettings, rawImage, 1f);
			}
		}
	}

	// this function is require to call to stop scanning when camSettings.scanType = ScanType.CONTINUOUS;
	// no need to call when camSettings.scanType = ScanType.ONCE;
	public void StopScanning()
	{
		QRCodeManager.Instance.StopScanning ();
	}

	string GetRearCamName()
	{
		foreach (WebCamDevice device in WebCamTexture.devices) {
			if (!device.isFrontFacing) {
				return device.name;
			}
		}
		return null;
	}

	// scene loading
	public void OnPayloadGeneratorClick()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene (1);
	}

}

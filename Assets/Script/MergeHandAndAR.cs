using UnityEngine;
using System.Collections;
using OpenCVForUnityExample;
using OpenCVForUnity;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Vuforia;

public class MergeHandAndAR : MonoBehaviour {

    private Scalar blobColorHsv;
    private ColorBlobDetector detector;
    private Mat spectrumMat;
    private bool isColorSelected = false;
    private Size SPECTRUM_SIZE;
    private Scalar CONTOUR_COLOR;
    private Scalar CONTOUR_COLOR_WHITE;
    int numberOfFingers = 0;
    Point storedTouchPoint;
    bool isInited = false;
    bool m_RegisteredFormat = false;
    private Image.PIXEL_FORMAT m_PixelFormat = Image.PIXEL_FORMAT.GRAYSCALE;
    public GameObject quad;
    public GameObject PlaneBackground;
    public Camera mainCamera;
    public Camera mainCameraViewObject;
    public Camera mainCameraAR;
	public double pointaX;
	public double pointaY;
	public double pointbX;
	public double pointbY;
    Mat cameraMat;
    Texture2D cameraTexture;
    Texture2D outputTexture;
    Color32[] colors;
    Mat mSepiaKernel;
    private Size mSize0;
    private Mat mIntermediateMat;
	Vector3[] vectors;
	float[] xFloats;
	float[] yFloats;
	float[] zFloats;


    void Start()
    {
        cameraMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC4);
        Debug.Log("imgMat dst ToString " + cameraMat.ToString());
        cameraTexture = new Texture2D(cameraMat.cols(), cameraMat.rows(), TextureFormat.ARGB32, false);
        outputTexture = new Texture2D(cameraMat.cols(), cameraMat.rows(), TextureFormat.ARGB32, false);
        colors = new Color32[outputTexture.width * outputTexture.height];
        mainCamera.orthographicSize = cameraTexture.height / 2;
        quad.transform.localScale = new Vector3(cameraTexture.width, cameraTexture.height, quad.transform.localScale.z);
        quad.GetComponent<Renderer>().material.mainTexture = outputTexture;
        mSepiaKernel = new Mat(4, 4, CvType.CV_32F);
        mSepiaKernel.put(0, 0, /* R */0.189f, 0.769f, 0.393f, 0f);
        mSepiaKernel.put(1, 0, /* G */0.168f, 0.686f, 0.349f, 0f);
        mSepiaKernel.put(2, 0, /* B */0.131f, 0.534f, 0.272f, 0f);
        mSepiaKernel.put(3, 0, /* A */0.000f, 0.000f, 0.000f, 1f);
        OnWebCamTextureToMatHelperInited(cameraMat);
        mIntermediateMat = new Mat();
        mSize0 = new Size();
        Invoke("InitCameraView3D", 0.5f);
    }

    private void InitCameraView3D()
    {
        mainCameraViewObject.orthographicSize = mainCameraAR.orthographicSize;
        mainCameraViewObject.fieldOfView = mainCameraAR.fieldOfView;
        mainCameraViewObject.nearClipPlane = mainCameraAR.nearClipPlane;
        mainCameraViewObject.farClipPlane = mainCameraAR.farClipPlane;
    }
		
    void Update()
    {
        OnTouchScreen();
    }
		
    void OnPostRender()
    {
        UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, cameraTexture.width, cameraTexture.height);
        cameraTexture.ReadPixels(rect, 0, 0, true);
        Utils.texture2DToMat(cameraTexture, cameraMat);
        HandArea(cameraMat);
    }

    private void HandArea(Mat cameraMat)
    {
        if (storedTouchPoint != null)
        {
            onTouch(cameraMat, convertScreenPoint(storedTouchPoint, quad, Camera.main));
            storedTouchPoint = null;
        }

        handPoseEstimationProcess(cameraMat);

        Imgproc.putText(cameraMat, "Please touch the area of the open hand.",
           new Point(5, cameraMat.rows() - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0,
           new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

        //              Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

        Utils.matToTexture2D(cameraMat, outputTexture, colors);
    }

    /// <summary>
    /// Hands the pose estimation process.
    /// </summary>
    public void handPoseEstimationProcess(Mat rgbaMat)
    {
        //Imgproc.blur(mRgba, mRgba, new Size(5,5));
        Imgproc.GaussianBlur(rgbaMat, rgbaMat, new OpenCVForUnity.Size(3, 3), 1, 1);
        //Imgproc.medianBlur(mRgba, mRgba, 3);

        if (!isColorSelected)
            return;

        List<MatOfPoint> contours = detector.getContours();
        detector.process(rgbaMat);
		//Debug.Log(contours + " | " + contours.Count);
		//string[] output = contours.ToArray();
	
		for(int i = 0; i < contours.Count; i++)
		{
			//Debug.Log("MatOfPoint2f " + new MatOfPoint2f(contours[i].toArray()) + " | " + i);
			//Debug.Log("MatOfPoint " + contours [i] + " | " + i);
			//Imgproc.circle(rgbaMat, contours[i], 6, new Scalar(0, 255, 0, 255), -1);


			//Debug.Log ("kotka" +  MatOfPoint.ReferenceEquals(x, y));

		}

		if (contours.Count <= 0)
        {
            return;
        }


        RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

        double boundWidth = rect.size.width;
        double boundHeight = rect.size.height;
        int boundPos = 0;

        for (int i = 1; i < contours.Count; i++)
        {
            rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
            if (rect.size.width * rect.size.height > boundWidth * boundHeight)
            {
                boundWidth = rect.size.width;
                boundHeight = rect.size.height;
                boundPos = i;
            }
        }
			
        OpenCVForUnity.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contours[boundPos].toArray()));
        Imgproc.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), CONTOUR_COLOR_WHITE, 2, 8, 0);
		//tochkaX = boundRect.tl ().x;
		//tochkaY = boundRect.tl ().y;
		Imgproc.circle(rgbaMat, boundRect.tl() , 6, new Scalar(0, 255, 0, 255), -1);
		Imgproc.circle(rgbaMat, boundRect.br() , 6, new Scalar(0, 255, 0, 255), -1);
		pointbX = boundRect.br ().x;
		pointbY =  boundRect.br ().y;
		pointaX =  boundRect.x;
		pointbY = boundRect.y;
        double a = boundRect.br().y - boundRect.tl().y;
        a = a * 0.7;
        a = boundRect.tl().y + a;
        Imgproc.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), CONTOUR_COLOR, 2, 8, 0);
        MatOfPoint2f pointMat = new MatOfPoint2f();
        Imgproc.approxPolyDP(new MatOfPoint2f(contours[boundPos].toArray()), pointMat, 3, true);
        contours[boundPos] = new MatOfPoint(pointMat.toArray());
        MatOfInt hull = new MatOfInt();
        MatOfInt4 convexDefect = new MatOfInt4();
        Imgproc.convexHull(new MatOfPoint(contours[boundPos].toArray()), hull);
        if (hull.toArray().Length < 3)
            return;
        Imgproc.convexityDefects(new MatOfPoint(contours[boundPos].toArray()), hull, convexDefect);
        List<MatOfPoint> hullPoints = new List<MatOfPoint>();
        List<Point> listPo = new List<Point>();
        for (int j = 0; j < hull.toList().Count; j++)
        {
            listPo.Add(contours[boundPos].toList()[hull.toList()[j]]);
        }
        MatOfPoint e = new MatOfPoint();
        e.fromList(listPo);
        hullPoints.Add(e);
        List<MatOfPoint> defectPoints = new List<MatOfPoint>();
        List<Point> listPoDefect = new List<Point>();
        for (int j = 0; j < convexDefect.toList().Count; j = j + 4)
        {
            Point farPoint = contours[boundPos].toList()[convexDefect.toList()[j + 2]];
            int depth = convexDefect.toList()[j + 3];
            if (depth > 8700 && farPoint.y < a)
            {
                listPoDefect.Add(contours[boundPos].toList()[convexDefect.toList()[j + 2]]);
            }
        }

        MatOfPoint e2 = new MatOfPoint();
        e2.fromList(listPo);
        defectPoints.Add(e2);
        Imgproc.drawContours(rgbaMat, hullPoints, -1, CONTOUR_COLOR, 3);
        this.numberOfFingers = listPoDefect.Count;
        if (this.numberOfFingers > 5)
            this.numberOfFingers = 5;
        foreach (Point p in listPoDefect)
        {
            Imgproc.circle(rgbaMat, p, 6, new Scalar(255, 0, 255, 255), -1);
        }
    }
		
    public void onTouch(Mat rgbaMat, Point touchPoint)
    {
        int cols = rgbaMat.cols();
        int rows = rgbaMat.rows();
        int x = (int)touchPoint.x;
        int y = (int)touchPoint.y;
        if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
            return;
        OpenCVForUnity.Rect touchedRect = new OpenCVForUnity.Rect();
        touchedRect.x = (x > 5) ? x - 5 : 0;
        touchedRect.y = (y > 5) ? y - 5 : 0;
        touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
        touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;
        Mat touchedRegionRgba = rgbaMat.submat(touchedRect);
        Mat touchedRegionHsv = new Mat();
        Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);
        blobColorHsv = Core.sumElems(touchedRegionHsv);
        int pointCount = touchedRect.width * touchedRect.height;
        for (int i = 0; i < blobColorHsv.val.Length; i++)
            blobColorHsv.val[i] /= pointCount;
        detector.setHsvColor(blobColorHsv);
        Imgproc.resize(detector.getSpectrum(), spectrumMat, SPECTRUM_SIZE);
        isColorSelected = true;
        touchedRegionRgba.release();
        touchedRegionHsv.release();
    }

 
    Point convertScreenPoint(Point screenPoint, GameObject quad, Camera cam)
    {
        Vector2 tl;
        Vector2 tr;
        Vector2 br; 
        Vector2 bl;
        tl = cam.WorldToScreenPoint(
            new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2,
            quad.transform.localPosition.y + quad.transform.localScale.y / 2,
            quad.transform.localPosition.z));
        tr = cam.WorldToScreenPoint(
            new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2,
            quad.transform.localPosition.y + quad.transform.localScale.y / 2,
            quad.transform.localPosition.z));
        br = cam.WorldToScreenPoint(
            new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2,
            quad.transform.localPosition.y - quad.transform.localScale.y / 2,
            quad.transform.localPosition.z));
        bl = cam.WorldToScreenPoint(
            new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2,
            quad.transform.localPosition.y - quad.transform.localScale.y / 2,
            quad.transform.localPosition.z));
        Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2);
        srcRectMat.put(0, 0, 0, cameraTexture.height, cameraTexture.width,
            cameraTexture.height, cameraTexture.width, 0, 0, 0);
        dstRectMat.put(0, 0, 0.0, 0.0, cameraTexture.width,
            0.0, cameraTexture.width, cameraTexture.height,
            0.0, cameraTexture.height);
        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat);
        MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint);
        MatOfPoint2f dstPointMat = new MatOfPoint2f();
        Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);
        return dstPointMat.toArray()[0];
    }

    private void OnTouchScreen()
    {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)){
                    storedTouchPoint = new Point (t.position.x, t.position.y);
                    Debug.Log ("touch X " + t.position.x);
                    Debug.Log ("touch Y " + t.position.y);
                }
            }
#else
        if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
		{
            storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
        }
#endif
    }

    public void OnWebCamTextureToMatHelperInited(Mat textureMat)
    {
        Mat webCamTextureMat = textureMat;
        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();
		detector = new ColorBlobDetector();
        spectrumMat = new Mat();
        blobColorHsv = new Scalar(255);
        SPECTRUM_SIZE = new Size(200, 64);
        CONTOUR_COLOR = new Scalar(255, 0, 0, 255);
        CONTOUR_COLOR_WHITE = new Scalar(255, 255, 255, 255);
    }
}




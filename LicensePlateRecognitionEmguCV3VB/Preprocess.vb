'Preprocess.vb
'
'Emgu CV 3.0.0

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '
Imports Emgu.CV.Util                '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module Preprocess

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const GAUSSIAN_BLUR_FILTER_SIZE As Integer = 5
    Const ADAPTIVE_THRESH_BLOCK_SIZE As Integer = 19
    Const ADAPTIVE_THRESH_WEIGHT As Integer = 9

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub preprocess(imgOriginal As Mat, ByRef imgGrayscale As Mat, ByRef imgThresh As Mat)
        imgGrayscale = extractValue(imgOriginal)                                'extract value channel only from original image to get imgGrayscale

        Dim imgMaxContrastGrayscale As Mat = maximizeContrast(imgGrayscale)         'maximize contrast with top hat and black hat
        
        Dim imgBlurred As New Mat()

        CvInvoke.GaussianBlur(imgMaxContrastGrayscale, imgBlurred, New Size(GAUSSIAN_BLUR_FILTER_SIZE, GAUSSIAN_BLUR_FILTER_SIZE), 0)       'gaussian blur
        
                    'adaptive threshold to get imgThresh
        CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255.0, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, ADAPTIVE_THRESH_BLOCK_SIZE, ADAPTIVE_THRESH_WEIGHT)
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractValue(imgOriginal As Mat) As Mat
        Dim imgHSV As New Mat()
        Dim vectorOfHSVImages As New VectorOfMat()
        Dim imgValue As New Mat()
        
        CvInvoke.CvtColor(imgOriginal, imgHSV, ColorConversion.Bgr2Hsv)

        CvInvoke.Split(imgHSV, vectorOfHSVImages)

        imgValue = vectorOfHSVImages(2)
        
        Return imgValue
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function maximizeContrast(imgGrayscale As Mat) As Mat
        Dim imgTopHat As New Mat()
	    Dim imgBlackHat As New Mat()
	    Dim imgGrayscalePlusTopHat As New Mat()
	    Dim imgGrayscalePlusTopHatMinusBlackHat As New Mat()

        Dim structuringElement As Mat = CvInvoke.GetStructuringElement(ElementShape.Rectangle, New Size(3, 3), New Point(-1, -1))

        CvInvoke.MorphologyEx(imgGrayscale, imgTopHat, MorphOp.Tophat, structuringElement, New Point(-1, -1), 1, BorderType.Default, New MCvScalar())
        CvInvoke.MorphologyEx(imgGrayscale, imgBlackHat, MorphOp.Blackhat, structuringElement, New Point(-1, -1), 1, BorderType.Default, New MCvScalar())

        CvInvoke.Add(imgGrayscale, imgTopHat, imgGrayscalePlusTopHat)
        CvInvoke.Subtract(imgGrayscalePlusTopHat, imgBlackHat, imgGrayscalePlusTopHatMinusBlackHat)
        
        Return imgGrayscalePlusTopHatMinusBlackHat
    End Function
    
End Module

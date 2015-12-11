'DetectPlates.vb
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
Module DetectPlates

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const PLATE_WIDTH_PADDING_FACTOR As Double = 1.5
    Const PLATE_HEIGHT_PADDING_FACTOR As Double = 1.5

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlatesInScene(imgOriginalScene As Mat) As List(Of PossiblePlate)
        Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)         'this will be the return value

        Dim imgGrayscaleScene As New Mat()
        Dim imgThreshScene As New Mat()
        Dim imgContours As New Mat(imgOriginalScene.Size, DepthType.Cv8U, 1)
        Dim random As New Random()

        CvInvoke.DestroyAllWindows()

        If (frmMain.cbShowSteps.Checked = True) Then
            CvInvoke.Imshow("0", imgOriginalScene)
        End If

        Preprocess.preprocess(imgOriginalScene, imgGrayscaleScene, imgThreshScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            CvInvoke.Imshow("1a", imgGrayscaleScene)
            CvInvoke.Imshow("1b", imgThreshScene)
        End If

        Dim listOfPossibleCharsInScene As List(Of PossibleChar) = findPossibleCharsInScene(imgThreshScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText("step 2 - listOfPossibleCharsInScene.Count = " + listOfPossibleCharsInScene.Count.ToString + vbCrLf)     '175 with MCLRNF1 image

            Dim contours As New VectorOfVectorOfPoint()

            For Each possibleChar As PossibleChar In listOfPossibleCharsInScene
                contours.Push(possibleChar.contour)
            Next

            CvInvoke.DrawContours(imgContours, contours, -1, New MCvScalar(255.0))
            CvInvoke.Imshow("2b", imgContours)
        End If

        Dim listOfListsOfMatchingCharsInScene As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleCharsInScene)

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText("step 3 - listOfListsOfMatchingCharsInScene.Count = " + listOfListsOfMatchingCharsInScene.Count.ToString + vbCrLf)     '13 with MCLRNF1 image
            
            imgContours = New Mat(imgOriginalScene.Size, DepthType.Cv8U, 1)

            For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInScene
                Dim intRandomBlue = random.Next(0, 256)
                Dim intRandomGreen = random.Next(0, 256)
                Dim intRandomRed = random.Next(0, 256)

                For Each matchingChar As PossibleChar In listOfMatchingChars
                    CvInvoke.DrawContours(imgContours, matchingChar.contour, 0, New MCvScalar(CDbl(intRandomBlue), CDbl(intRandomGreen), CDbl(intRandomRed)))
                Next
            Next
            CvInvoke.Imshow("3", imgContours)
        End If

        For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInScene
            Dim possiblePlate = extractPlate(imgOriginalScene, listOfMatchingChars)

            If (Not possiblePlate.imgPlate Is Nothing) Then
                listOfPossiblePlates.Add(possiblePlate)
            End If
        Next

        frmMain.txtInfo.AppendText(vbCrLf + listOfPossiblePlates.Count.ToString + " possible plates found" + vbCrLf)

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText(vbCrLf)
            CvInvoke.Imshow("4a", imgContours)

            For i As Integer = 0 To listOfPossiblePlates.Count - 1
                Dim ptfRectPoints(4) As PointF

                ptfRectPoints = listOfPossiblePlates(i).rrLocationOfPlateInScene.GetVertices()

                Dim pt0 As New Point(CInt(ptfRectPoints(0).X), CInt(ptfRectPoints(0).Y))
                Dim pt1 As New Point(CInt(ptfRectPoints(1).X), CInt(ptfRectPoints(1).Y))
                Dim pt2 As New Point(CInt(ptfRectPoints(2).X), CInt(ptfRectPoints(2).Y))
                Dim pt3 As New Point(CInt(ptfRectPoints(3).X), CInt(ptfRectPoints(3).Y))
            
                CvInvoke.Line(imgContours, pt0, pt1, New MCvScalar(0.0, 0.0, 255.0), 2)
                CvInvoke.Line(imgContours, pt1, pt2, New MCvScalar(0.0, 0.0, 255.0), 2)
                CvInvoke.Line(imgContours, pt2, pt3, New MCvScalar(0.0, 0.0, 255.0), 2)
                CvInvoke.Line(imgContours, pt3, pt0, New MCvScalar(0.0, 0.0, 255.0), 2)

                CvInvoke.Imshow("4a", imgContours)
                frmMain.txtInfo.AppendText("possible plate " + i.ToString + ", click on any image and press a key to continue . . ." + vbCrLf)
                CvInvoke.Imshow("4b", listOfPossiblePlates(i).imgPlate)
                CvInvoke.WaitKey(0)
            Next
            frmMain.txtInfo.AppendText(vbCrLf + "plate detection complete, click on any image and press a key to begin char recognition . . ." + vbCrLf + vbCrLf)
            CvInvoke.WaitKey(0)
        End If
        
        Return listOfPossiblePlates
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInScene(imgThresh As Mat) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()      'this is the return value

        Dim imgContours As New Mat(imgThresh.Size(), DepthType.Cv8U, 1)
        Dim intCountOfPossibleChars As Integer = 0

        Dim imgThreshCopy As Mat = imgThresh.Clone()

        Dim contours As New VectorOfVectorOfPoint()

        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)

        For i As Integer = 0 To contours.Size() - 1
            If (frmMain.cbShowSteps.Checked = True) Then
                CvInvoke.DrawContours(imgContours, contours, i, New MCvScalar(255.0))
            End If
            
            Dim possibleChar As New PossibleChar(contours(i))

            If (DetectChars.checkIfPossibleChar(possibleChar)) Then
                intCountOfPossibleChars = intCountOfPossibleChars + 1
                listOfPossibleChars.Add(possibleChar)
            End If
            
        Next

        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText(vbCrLf + "step 2 - contours.Size() = " + contours.Size().ToString + vbCrLf)      '2362 with MCLRNF1 image
            frmMain.txtInfo.AppendText("step 2 - intCountOfPossibleChars = " + intCountOfPossibleChars.ToString + vbCrLf)     '175 with MCLRNF1 image
            CvInvoke.imshow("2a", imgContours)
        End If
        
        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractPlate(imgOriginal As Mat, listOfMatchingChars As List(Of PossibleChar)) As PossiblePlate
        Dim possiblePlate As PossiblePlate = New PossiblePlate          'this will be the return value

        listOfMatchingChars.Sort(Function(firstChar, secondChar) firstChar.intCenterX.CompareTo(secondChar.intCenterX))
        
        Dim dblPlateCenterX As Double = CDbl(listOfMatchingChars(0).intCenterX + listOfMatchingChars(listOfMatchingChars.Count - 1).intCenterX) / 2.0
        Dim dblPlateCenterY As Double = CDbl(listOfMatchingChars(0).intCenterY + listOfMatchingChars(listOfMatchingChars.Count - 1).intCenterY) / 2.0

        Dim ptfPlateCenter As New PointF(CSng(dblPlateCenterX), CSng(dblPlateCenterY))

        Dim intPlateWidth As Integer = CInt(CDbl(listOfMatchingChars(listOfMatchingChars.Count - 1).boundingRect.X + listOfMatchingChars(listOfMatchingChars.Count - 1).boundingRect.Width - listOfMatchingChars(0).boundingRect.X) * PLATE_WIDTH_PADDING_FACTOR)

        Dim intTotalOfCharHeights As Integer = 0

        For Each matchingChar As PossibleChar In listOfMatchingChars
            intTotalOfCharHeights = intTotalOfCharHeights + matchingChar.boundingRect.Height
        Next

        Dim dblAverageCharHeight = CDbl(intTotalOfCharHeights) / CDbl(listOfMatchingChars.Count)

        Dim intPlateHeight = CInt(dblAverageCharHeight * PLATE_HEIGHT_PADDING_FACTOR)

        Dim dblOpposite As Double = listOfMatchingChars(listOfMatchingChars.Count - 1).intCenterY - listOfMatchingChars(0).intCenterY
        Dim dblHypotenuse As Double = DetectChars.distanceBetweenChars(listOfMatchingChars(0), listOfMatchingChars(listOfMatchingChars.Count - 1))
        Dim dblCorrectionAngleInRad As Double = Math.Asin(dblOpposite / dblHypotenuse)
        Dim dblCorrectionAngleInDeg As Double = dblCorrectionAngleInRad * (180.0 / Math.PI)

        Dim rotationMatrix As New Mat()

        CvInvoke.GetRotationMatrix2D(ptfPlateCenter, dblCorrectionAngleInDeg, 1.0, rotationMatrix)

        CvInvoke.WarpAffine(possiblePlate.imgPlate, possiblePlate.imgPlate, rotationMatrix, possiblePlate.imgPlate.Size)
        
        Return possiblePlate
    End Function

End Module

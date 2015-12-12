'DetectChars.vb
'
'Emgu CV 2.4.10

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '
Imports Emgu.CV.ML                  '
Imports Emgu.CV.Util

Imports System.Xml
Imports System.Xml.Serialization    'these imports are for reading Matrix objects from file
Imports System.IO

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectChars
    
    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                                'constants for checkIfPossibleChar, this checks one possible char only (does not compare to another char)
    Const MIN_PIXEL_WIDTH As Integer = 2
    Const MIN_PIXEL_HEIGHT As Integer = 8

    Const MIN_ASPECT_RATIO As Double = 0.25
    Const MAX_ASPECT_RATIO As Double = 1.0

    Const MIN_RECT_AREA As Integer = 80

                                'constants for comparing two chars
    Const MIN_DIAG_SIZE_MULTIPLE_AWAY As Double = 0.3
    Const MAX_DIAG_SIZE_MULTIPLE_AWAY As Double = 5.0

    Const MAX_CHANGE_IN_AREA As Double = 0.5

    Const MAX_CHANGE_IN_WIDTH As Double = 0.8
    Const MAX_CHANGE_IN_HEIGHT As Double = 0.2

    Const MAX_ANGLE_BETWEEN_CHARS As Double = 12.0

                                'other constants
    Const MIN_NUMBER_OF_MATCHING_CHARS As Integer = 3

    Const RESIZED_CHAR_IMAGE_WIDTH As Integer = 20
    Const RESIZED_CHAR_IMAGE_HEIGHT As Integer = 30
    
    Dim SCALAR_WHITE As New MCvScalar(255.0, 255.0, 255.0)
    Dim SCALAR_GREEN As New MCvScalar(0.0, 255.0, 0.0)

                                'variables
    Dim kNearest As New KNearest()
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function loadKNNDataAndTrainKNN() As Boolean
                'note: we effectively have to read the first XML file twice
                'first, we read the file to get the number of rows (which is the same as the number of samples)
                'the first time reading the file we can't get the data yet, since we don't know how many rows of data there are
                'next, reinstantiate our classifications Matrix and training images Matrix with the correct number of rows
                'then, read the file again and this time read the data into our resized classifications Matrix and training images Matrix

        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(1, 1)           'for the first time through, declare these to be 1 row by 1 column
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(1, 1)            'we will resize these when we know the number of rows (i.e. number of training samples)

        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )
        
        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)              'these variables are for
        Dim streamReader As StreamReader                                                                'reading from the XML files

        Try
            streamReader = new StreamReader("classifications.xml")              'attempt to open classifications file
        Catch ex As Exception                                                   'if error is encountered, show error and return
            frmMain.txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error: ")
            frmMain.txtInfo.AppendText(ex.Message + vbCrLf)
            Return False
        End Try

                'read from the classifications file the 1st time, this is only to get the number of rows, not the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()            'close the classifications XML file

        Dim intNumberOfTrainingSamples As Integer = mtxClassifications.Rows             'get the number of rows, i.e. the number of training samples

                'now that we know the number of rows, reinstantiate classifications Matrix and training images Matrix with the actual number of rows
        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_CHAR_IMAGE_WIDTH * RESIZED_CHAR_IMAGE_HEIGHT)

        Try
            streamReader = new StreamReader("classifications.xml")              'reinitialize the stream reader
        Catch ex As Exception                                                   'if error is encountered, show error and return
            frmMain.txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error:" + vbCrLf)
            frmMain.txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
            Return False
        End Try
                        'read from the classifications file again, this time we can get the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()            'close the classifications XML file

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)                'reinstantiate file reading variable

        Try
            streamReader = New StreamReader("images.xml")
        Catch ex As Exception                                               'if error is encountered, show error and return
            frmMain.txtInfo.AppendText("unable to open 'images.xml', error:" + vbCrLf)
            frmMain.txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
            Return False
        End Try

        mtxTrainingImages = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))           'read from training images file
        streamReader.Close()                                            'close the training images XML file

                    ' train '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        
        kNearest.DefaultK = 1

        kNearest.Train(mtxTrainingImages, MlEnum.DataLayoutType.RowSample, mtxClassifications)
        
        Return True
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectCharsInPlates(listOfPossiblePlates As List(Of PossiblePlate)) As List(Of PossiblePlate)
        Dim intPlateCounter As Integer = 0              'this is only for showing steps
        Dim imgContours As Mat
        Dim random As New Random()                      'this is only for showing steps

        If (listOfPossiblePlates Is Nothing) Then           'if list of possible plates is null,
            Return listOfPossiblePlates                     'return
        ElseIf (listOfPossiblePlates.Count = 0) Then        'if list of possible plates has zero plates
            Return listOfPossiblePlates                     'return
        End If
                        'at this point we can be sure list of possible plates has at least one plate

        For Each possiblePlate As PossiblePlate In listOfPossiblePlates
            Preprocess.preprocess(possiblePlate.imgPlate, possiblePlate.imgGrayscale, possiblePlate.imgThresh)

            If (frmMain.cbShowSteps.Checked = True) Then
                CvInvoke.Imshow("5a", possiblePlate.imgPlate)
                CvInvoke.Imshow("5b", possiblePlate.imgGrayscale)
                CvInvoke.Imshow("5c", possiblePlate.imgThresh)
            End If

            CvInvoke.Resize(possiblePlate.imgThresh, possiblePlate.imgThresh, New Size(), 1.6, 1.6)

            CvInvoke.Threshold(possiblePlate.imgThresh, possiblePlate.imgThresh, 0.0, 255.0, ThresholdType.Binary Or ThresholdType.Otsu)

            If (frmMain.cbShowSteps.Checked = True) Then
                CvInvoke.Imshow("5d", possiblePlate.imgThresh)
            End If

            Dim listOfPossibleCharsInPlate As List(Of PossibleChar) = findPossibleCharsInPlate(possiblePlate.imgGrayscale, possiblePlate.imgThresh)

            If (frmMain.cbShowSteps.Checked = True) Then
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)
                Dim contours As New VectorOfVectorOfPoint()

                For Each possibleChar As PossibleChar In listOfPossibleCharsInPlate
                    contours.Push(possibleChar.contour)
                Next

                CvInvoke.DrawContours(imgContours, contours, -1, SCALAR_WHITE)

                CvInvoke.Imshow("6", imgContours)
                
            End If

            Dim listOfListsOfMatchingCharsInPlate As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleCharsInPlate)

            If (frmMain.cbShowSteps.Checked = True) Then
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)

                Dim contours As New VectorOfVectorOfPoint()

                For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInPlate
                    Dim intRandomBlue = random.Next(0, 256)
                    Dim intRandomGreen = random.Next(0, 256)
                    Dim intRandomRed = random.Next(0, 256)

                    For Each matchingChar As PossibleChar In listOfMatchingChars
                        contours.Push(matchingChar.contour)
                    Next
                    CvInvoke.DrawContours(imgContours, contours, -1, New MCvScalar(CDbl(intRandomBlue), CDbl(intRandomGreen), CDbl(intRandomRed)))
                Next

                CvInvoke.Imshow("7", imgContours)
            End If

            If (listOfListsOfMatchingCharsInPlate Is Nothing) Then
                If (frmMain.cbShowSteps.Checked = True) Then
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.DestroyWindow("8")
                    CvInvoke.DestroyWindow("9")
                    CvInvoke.DestroyWindow("10")
                    CvInvoke.WaitKey(0)
                End If

                possiblePlate.strChars = ""
                Continue For
            ElseIf (listOfListsOfMatchingCharsInPlate.Count = 0) Then
                If (frmMain.cbShowSteps.Checked = True) Then
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.DestroyWindow("8")
                    CvInvoke.DestroyWindow("9")
                    CvInvoke.DestroyWindow("10")
                    CvInvoke.WaitKey(0)
                End If

                possiblePlate.strChars = ""
                Continue For
            End If

            For i As Integer = 0 To listOfListsOfMatchingCharsInPlate.Count - 1
                listOfListsOfMatchingCharsInPlate(i).Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))
                listOfListsOfMatchingCharsInPlate(i) = removeInnerOverlappingChars(listOfListsOfMatchingCharsInPlate(i))
            Next

            If (frmMain.cbShowSteps.Checked = True) Then
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)

                For Each listOfMatchingChars As List(Of PossibleChar) In listOfListsOfMatchingCharsInPlate
                    Dim intRandomBlue = random.Next(0, 256)
                    Dim intRandomGreen = random.Next(0, 256)
                    Dim intRandomRed = random.Next(0, 256)

                    Dim contours As New VectorOfVectorOfPoint()

                    For Each matchingChar As PossibleChar In listOfMatchingChars
                        contours.Push(matchingChar.contour)
                    Next
                    CvInvoke.DrawContours(imgContours, contours, -1, New MCvScalar(CDbl(intRandomBlue), CDbl(intRandomGreen), CDbl(intRandomRed)))
                Next
                CvInvoke.Imshow("8", imgContours)
            End If
                            'within each possible plate, suppose the longest list of potential matching chars is the actual list of chars
            Dim intLenOfLongestListOfChars As Integer = 0
            Dim intIndexOfLongestListOfChars As Integer = 0
                                                            'loop through all the lists of matching chars, get the index of the one with the most chars
            For i As Integer = 0 To listOfListsOfMatchingCharsInPlate.Count - 1
                If (listOfListsOfMatchingCharsInPlate(i).Count > intLenOfLongestListOfChars) Then
                    intLenOfLongestListOfChars = listOfListsOfMatchingCharsInPlate(i).Count
                    intIndexOfLongestListOfChars = i
                End If
            Next

            Dim longestListOfMatchingCharsInPlate As List(Of PossibleChar) = listOfListsOfMatchingCharsInPlate(intIndexOfLongestListOfChars)

            If (frmMain.cbShowSteps.Checked = True) Then
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)

                Dim contours As New VectorOfVectorOfPoint()

                For Each matchingChar As PossibleChar In longestListOfMatchingCharsInPlate
                    contours.Push(matchingChar.contour)
                Next

                CvInvoke.DrawContours(imgContours, contours, -1, SCALAR_WHITE)

                CvInvoke.Imshow("9", imgContours)
            End If

            possiblePlate.strChars = recognizeCharsInPlate(possiblePlate.imgThresh, longestListOfMatchingCharsInPlate)

            If (frmMain.cbShowSteps.Checked = True) Then
                frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = " + possiblePlate.strChars + ", click on any image and press a key to continue . . ." + vbCrLf)
                intPlateCounter = intPlateCounter + 1
                CvInvoke.WaitKey(0)
            End If
        Next
        
        If (frmMain.cbShowSteps.Checked = True) Then
            frmMain.txtInfo.AppendText(vbCrLf + "char detection complete, click on any image and press a key to continue . . ." + vbCrLf)
            CvInvoke.WaitKey(0)
        End If
        
        Return listOfPossiblePlates
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInPlate(imgGrayscale As Mat, imgThresh As Mat) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)        'this will be the return value

        Dim imgThreshCopy As New Mat()

        Dim contours As New VectorOfVectorOfPoint()

        imgThreshCopy = imgThresh.Clone()

        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)

        For i As Integer = 0 To contours.Size - 1
            Dim possibleChar As New PossibleChar(contours(i))

            If (checkIfPossibleChar(possibleChar)) Then
                listOfPossibleChars.Add(possibleChar)
            End If

        Next
        
        Return listOfPossibleChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfPossibleChar(possibleChar As PossibleChar) As Boolean
        If (possibleChar.intRectArea > MIN_RECT_AREA And _
            possibleChar.boundingRect.Width > MIN_PIXEL_WIDTH And possibleChar.boundingRect.Height > MIN_PIXEL_HEIGHT And _
            MIN_ASPECT_RATIO < possibleChar.dblAspectRatio And possibleChar.dblAspectRatio < MAX_ASPECT_RATIO) Then
            Return True
        Else
            Return False
        End If
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfListsOfMatchingChars(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))
        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))       'this will be the return value

        For Each possibleChar As PossibleChar In listOfPossibleChars

            Dim listOfMatchingChars As List(Of PossibleChar) = findListOfMatchingChars(possibleChar, listOfPossibleChars)

            listOfMatchingChars.Add(possibleChar)

            If (listOfMatchingChars.Count < MIN_NUMBER_OF_MATCHING_CHARS) Then
                Continue For
            End If

            listOfListsOfMatchingChars.Add(listOfMatchingChars)

            Dim listOfPossibleCharsWithCurrentMatchesRemoved As List(Of PossibleChar) = listOfPossibleChars.Except(listOfMatchingChars).ToList()

            Dim recursiveListOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

            recursiveListOfListsOfMatchingChars = findListOfListsOfMatchingChars(listOfPossibleCharsWithCurrentMatchesRemoved)      'recursive call

            For Each recursiveListOfMatchingChars As List(Of PossibleChar) In recursiveListOfListsOfMatchingChars
                listOfListsOfMatchingChars.Add(recursiveListOfMatchingChars)
            Next
            Exit For
        Next

        Return listOfListsOfMatchingChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(possibleChar As PossibleChar, listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)            'this will be the return value

        For Each possibleMatchingChar As PossibleChar In listOfChars

            If (possibleMatchingChar.Equals(possibleChar)) Then
                Continue For
            End If

            Dim dblDistanceBetweenChars As Double = distanceBetweenChars(possibleChar, possibleMatchingChar)

            Dim dblAngleBetweenChars As Double = angleBetweenChars(possibleChar, possibleMatchingChar)

            Dim dblChangeInArea As Double = Math.Abs(possibleMatchingChar.intRectArea - possibleChar.intRectArea) / possibleChar.intRectArea
            
            Dim dblChangeInWidth As Double = Math.Abs(possibleMatchingChar.boundingRect.Width - possibleChar.boundingRect.Width) / possibleChar.boundingRect.Width
            Dim dblChangeInHeight As Double = Math.Abs(possibleMatchingChar.boundingRect.Height - possibleChar.boundingRect.Height) / possibleChar.boundingRect.Height

            If (dblDistanceBetweenChars < (possibleChar.dblDiagonalSize * MAX_DIAG_SIZE_MULTIPLE_AWAY) And _
                dblAngleBetweenChars < MAX_ANGLE_BETWEEN_CHARS And _
                dblChangeInArea < MAX_CHANGE_IN_AREA And _
                dblChangeInWidth < MAX_CHANGE_IN_WIDTH And _
                dblChangeInHeight < MAX_CHANGE_IN_HEIGHT) Then

                listOfMatchingChars.Add(possibleMatchingChar)
            End If
            
        Next
        
        Return listOfMatchingChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function distanceBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim intX As Integer = Math.Abs(firstChar.intCenterX - secondChar.intCenterX)
        Dim intY As Integer = Math.Abs(firstChar.intCenterY - secondChar.intCenterY)

        Return Math.Sqrt((intX ^ 2) + (intY ^ 2))
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function angleBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(firstChar.intCenterX - secondChar.intCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(firstChar.intCenterY - secondChar.intCenterY))

        Dim dblAngleInRad As Double = Math.Atan(dblOpp / dblAdj) 
        
        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / Math.PI)

        Return dblAngleInDeg
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function removeInnerOverlappingChars(listOfMatchingChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingCharsWithInnerCharRemoved As List(Of PossibleChar) = New List(Of PossibleChar)(listOfMatchingChars)

        For Each currentChar As PossibleChar In listOfMatchingChars
            For Each otherChar As PossibleChar In listOfMatchingChars
                If (Not currentChar.Equals(otherChar)) Then                                     'if current char and other char are not the same char . . .
                                                                                                'if current char and other char have center points at almost the same location . . .
                    If (distanceBetweenChars(currentChar, otherChar) < (currentChar.dblDiagonalSize * MIN_DIAG_SIZE_MULTIPLE_AWAY)) Then
                                        'if we get in here we have found overlapping chars
                                        'next we identify which char is smaller, then if that char was not already removed on a previous pass, remove it
                        If (currentChar.intRectArea < otherChar.intRectArea) Then                       'if current char is smaller than other char
                            If (listOfMatchingCharsWithInnerCharRemoved.Contains(currentChar)) Then     'if current char was not already removed on a previous pass . . .
                                listOfMatchingCharsWithInnerCharRemoved.Remove(currentChar)             'then remove current char
                            End If
                        Else                                                                            'else if other char is smaller than current char
                            If (listOfMatchingCharsWithInnerCharRemoved.Contains(otherChar)) Then       'if other char was not already removed on a previous pass . . .
                                listOfMatchingCharsWithInnerCharRemoved.Remove(otherChar)               'then remove other char
                            End If

                        End If
                    End If
                End If
            Next
        Next
        
        Return listOfMatchingCharsWithInnerCharRemoved
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function recognizeCharsInPlate(imgThresh As Mat, listOfMatchingChars As List(Of PossibleChar)) As String
        Dim strChars As String = ""         'this will be the return value, the chars in the lic plate

        Dim imgThreshColor As New Mat()

        listOfMatchingChars.Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))

        CvInvoke.CvtColor(imgThresh, imgThreshColor, ColorConversion.Gray2Bgr)

        For Each currentChar As PossibleChar In listOfMatchingChars
            CvInvoke.Rectangle(imgThreshColor, currentChar.boundingRect, SCALAR_GREEN, 2)

            Dim imgROItoBeCloned As New Mat(imgThresh, currentChar.boundingRect)

            Dim imgROI As Mat = imgROItoBeCloned.Clone()

            Dim imgROIResized As New Mat()

            CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_CHAR_IMAGE_WIDTH, RESIZED_CHAR_IMAGE_HEIGHT))

            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_CHAR_IMAGE_WIDTH * RESIZED_CHAR_IMAGE_HEIGHT)

            imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)

            For intRow As Integer = 0 To RESIZED_CHAR_IMAGE_HEIGHT - 1
                For intCol As Integer = 0 To RESIZED_CHAR_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_CHAR_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next

            Dim sngCurrentChar As Single

            sngCurrentChar = kNearest.Predict(mtxTempReshaped)

            strChars = strChars + Chr(Convert.ToInt32(sngCurrentChar))
        Next

        If (frmMain.cbShowSteps.Checked = True) Then
            CvInvoke.Imshow("10", imgThreshColor)
        End If
        
        Return strChars
    End Function
    
End Module


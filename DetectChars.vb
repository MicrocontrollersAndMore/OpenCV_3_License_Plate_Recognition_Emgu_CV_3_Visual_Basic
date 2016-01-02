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
        
        Return True         'if we got here training was successful so return true
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

        For Each possiblePlate As PossiblePlate In listOfPossiblePlates     ' for each possible plate, this is a big for loop that takes up most of the function
            Preprocess.preprocess(possiblePlate.imgPlate, possiblePlate.imgGrayscale, possiblePlate.imgThresh)      'preprocess to get grayscale and threshold images

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
                CvInvoke.Imshow("5a", possiblePlate.imgPlate)
                CvInvoke.Imshow("5b", possiblePlate.imgGrayscale)
                CvInvoke.Imshow("5c", possiblePlate.imgThresh)
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            CvInvoke.Resize(possiblePlate.imgThresh, possiblePlate.imgThresh, New Size(), 1.6, 1.6)     'upscale size by 60% for better viewing and character recognition

            CvInvoke.Threshold(possiblePlate.imgThresh, possiblePlate.imgThresh, 0.0, 255.0, ThresholdType.Binary Or ThresholdType.Otsu)    'threshold again to eliminate any gray areas

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
                CvInvoke.Imshow("5d", possiblePlate.imgThresh)
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                    'find all possible chars in the plate,
                    'this function first finds all contours, then only includes contours that could be chars (without comparison to other chars yet)
            Dim listOfPossibleCharsInPlate As List(Of PossibleChar) = findPossibleCharsInPlate(possiblePlate.imgGrayscale, possiblePlate.imgThresh)

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)
                Dim contours As New VectorOfVectorOfPoint()

                For Each possibleChar As PossibleChar In listOfPossibleCharsInPlate
                    contours.Push(possibleChar.contour)
                Next

                CvInvoke.DrawContours(imgContours, contours, -1, SCALAR_WHITE)

                CvInvoke.Imshow("6", imgContours)
                
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                    'given a list of all possible chars, find groups of matching chars within the plate
            Dim listOfListsOfMatchingCharsInPlate As List(Of List(Of PossibleChar)) = findListOfListsOfMatchingChars(listOfPossibleCharsInPlate)

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
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
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            If (listOfListsOfMatchingCharsInPlate Is Nothing) Then          'if no matching chars were found
                If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.DestroyWindow("8")
                    CvInvoke.DestroyWindow("9")
                    CvInvoke.DestroyWindow("10")
                    CvInvoke.WaitKey(0)
                End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                possiblePlate.strChars = ""             'set plate string member variable to empty string
                Continue For                            'and jump back to top of big for loop
            ElseIf (listOfListsOfMatchingCharsInPlate.Count = 0) Then
                If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''
                    frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = (none), click on any image and press a key to continue . . ." + vbCrLf)
                    intPlateCounter = intPlateCounter + 1
                    CvInvoke.DestroyWindow("8")
                    CvInvoke.DestroyWindow("9")
                    CvInvoke.DestroyWindow("10")
                    CvInvoke.WaitKey(0)
                End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                possiblePlate.strChars = ""             'set plate string member variable to empty string
                Continue For                            'and jump back to top of big for loop
            End If

            For i As Integer = 0 To listOfListsOfMatchingCharsInPlate.Count - 1         'for each group of chars within the plate

                            'sort chars from left to right
                listOfListsOfMatchingCharsInPlate(i).Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))

                            'remove inner overlapping chars
                listOfListsOfMatchingCharsInPlate(i) = removeInnerOverlappingChars(listOfListsOfMatchingCharsInPlate(i))
            Next

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
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
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

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

                        'suppose that the longest list of matching chars within the plate is the actual list of chars
            Dim longestListOfMatchingCharsInPlate As List(Of PossibleChar) = listOfListsOfMatchingCharsInPlate(intIndexOfLongestListOfChars)

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
                imgContours = New Mat(possiblePlate.imgThresh.Size, DepthType.Cv8U, 3)

                Dim contours As New VectorOfVectorOfPoint()

                For Each matchingChar As PossibleChar In longestListOfMatchingCharsInPlate
                    contours.Push(matchingChar.contour)
                Next

                CvInvoke.DrawContours(imgContours, contours, -1, SCALAR_WHITE)

                CvInvoke.Imshow("9", imgContours)
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            possiblePlate.strChars = recognizeCharsInPlate(possiblePlate.imgThresh, longestListOfMatchingCharsInPlate)      'perform char recognition on the longest list of matching chars in the plate

            If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''
                frmMain.txtInfo.AppendText("chars found in plate number " + intPlateCounter.ToString + " = " + possiblePlate.strChars + ", click on any image and press a key to continue . . ." + vbCrLf)
                intPlateCounter = intPlateCounter + 1
                CvInvoke.WaitKey(0)
            End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Next        'end for each possible plate big for loop that takes up most of the function
        
        If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''''''
            frmMain.txtInfo.AppendText(vbCrLf + "char detection complete, click on any image and press a key to continue . . ." + vbCrLf)
            CvInvoke.WaitKey(0)
        End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        
        Return listOfPossiblePlates
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInPlate(imgGrayscale As Mat, imgThresh As Mat) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)        'this will be the return value

        Dim imgThreshCopy As New Mat()

        Dim contours As New VectorOfVectorOfPoint()

        imgThreshCopy = imgThresh.Clone()

        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)     'find all contours in plate

        For i As Integer = 0 To contours.Size - 1                   'for each contour
            Dim possibleChar As New PossibleChar(contours(i))

            If (checkIfPossibleChar(possibleChar)) Then             'if contour is a possible char, note this does not compare to other chars (yet) . . .
                listOfPossibleChars.Add(possibleChar)               'add to list of possible chars
            End If

        Next
        
        Return listOfPossibleChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfPossibleChar(possibleChar As PossibleChar) As Boolean
                'this function is a 'first pass' that does a rough check on a contour to see if it could be a char,
                'note that we are not (yet) comparing the char to other chars to look for a group
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
                'with this function, we start off with all the possible chars in one big list
                'the purpose of this function is to re-arrange the one big list of chars into a list of lists of matching chars,
                'note that chars that are not found to be in a group of matches do not need to be considered further
        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))       'this will be the return value
        
        For Each possibleChar As PossibleChar In listOfPossibleChars        'for each possible char in the one big list of chars
            
                    'find all chars in the big list that match the current char
            Dim listOfMatchingChars As List(Of PossibleChar) = findListOfMatchingChars(possibleChar, listOfPossibleChars)

            listOfMatchingChars.Add(possibleChar)       'also add the current char to current possible list of matching chars

                    'if current possible list of matching chars is not long enough to constitute a possible plate
            If (listOfMatchingChars.Count < MIN_NUMBER_OF_MATCHING_CHARS) Then
                Continue For                    'jump back to the top of the for loop and try again with next char, note that it's not necessary
                                                'to save the list in any way since it did not have enough chars to be a possible plate
            End If
                                                                    'if we get here, the current list passed test as a "group" or "cluster" of matching chars
            listOfListsOfMatchingChars.Add(listOfMatchingChars)     'so add to our list of lists of matching chars
            
                    'remove the current list of matching chars from the big list so we don't use those same chars twice,
                    'make sure to make a new big list for this since we don't want to change the original big list
            Dim listOfPossibleCharsWithCurrentMatchesRemoved As List(Of PossibleChar) = listOfPossibleChars.Except(listOfMatchingChars).ToList()

                    'declare new list of lists of chars to get result from recursive call
            Dim recursiveListOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))

            recursiveListOfListsOfMatchingChars = findListOfListsOfMatchingChars(listOfPossibleCharsWithCurrentMatchesRemoved)      'recursive call

            For Each recursiveListOfMatchingChars As List(Of PossibleChar) In recursiveListOfListsOfMatchingChars       'for each list of matching chars found by recursive call
                listOfListsOfMatchingChars.Add(recursiveListOfMatchingChars)                'add to our original list of lists of matching chars
            Next
            Exit For                'jump out of for loop
        Next

        Return listOfListsOfMatchingChars           'return result
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(possibleChar As PossibleChar, listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
                'the purpose of this function is, given a possible char and a big list of possible chars,
                'find all chars in the big list that are a match for the single possible char, and return those matching chars as a list
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)            'this will be the return value

        For Each possibleMatchingChar As PossibleChar In listOfChars        'for each char in big list
            
                            'if the char we attempting to find matches for is the exact same char as the char in the big list we are currently checking
            If (possibleMatchingChar.Equals(possibleChar)) Then
                                    'then we should not include it in the list of matches b/c that would end up double including the current char
                Continue For        'so do not add to list of matches and jump back to top of for loop
            End If
                        'compute stuff to see if chars are a match
            Dim dblDistanceBetweenChars As Double = distanceBetweenChars(possibleChar, possibleMatchingChar)

            Dim dblAngleBetweenChars As Double = angleBetweenChars(possibleChar, possibleMatchingChar)

            Dim dblChangeInArea As Double = Math.Abs(possibleMatchingChar.intRectArea - possibleChar.intRectArea) / possibleChar.intRectArea
            
            Dim dblChangeInWidth As Double = Math.Abs(possibleMatchingChar.boundingRect.Width - possibleChar.boundingRect.Width) / possibleChar.boundingRect.Width
            Dim dblChangeInHeight As Double = Math.Abs(possibleMatchingChar.boundingRect.Height - possibleChar.boundingRect.Height) / possibleChar.boundingRect.Height
            
                    'check if chars match
            If (dblDistanceBetweenChars < (possibleChar.dblDiagonalSize * MAX_DIAG_SIZE_MULTIPLE_AWAY) And _
                dblAngleBetweenChars < MAX_ANGLE_BETWEEN_CHARS And _
                dblChangeInArea < MAX_CHANGE_IN_AREA And _
                dblChangeInWidth < MAX_CHANGE_IN_WIDTH And _
                dblChangeInHeight < MAX_CHANGE_IN_HEIGHT) Then

                listOfMatchingChars.Add(possibleMatchingChar)       'if the chars are a match, add the current char to list of matching chars
            End If
            
        Next
        
        Return listOfMatchingChars          'return result
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'use Pythagorean theorem to calculate distance between two chars
    Function distanceBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim intX As Integer = Math.Abs(firstChar.intCenterX - secondChar.intCenterX)
        Dim intY As Integer = Math.Abs(firstChar.intCenterY - secondChar.intCenterY)

        Return Math.Sqrt((intX ^ 2) + (intY ^ 2))
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'use basic trigonometry (SOH CAH TOA) to calculate angle between chars
    Function angleBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(firstChar.intCenterX - secondChar.intCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(firstChar.intCenterY - secondChar.intCenterY))

        Dim dblAngleInRad As Double = Math.Atan(dblOpp / dblAdj) 
        
        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / Math.PI)

        Return dblAngleInDeg
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'if we have two chars overlapping or to close to each other to possibly be separate chars, remove the inner (smaller) char,
    'this is to prevent including the same char twice if two contours are found for the same char,
    'for example for the letter 'O' both the inner ring and the outer ring may be found as contours, but we should only include the char once
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
    'this is where we apply the actual char recognition
    Function recognizeCharsInPlate(imgThresh As Mat, listOfMatchingChars As List(Of PossibleChar)) As String
        Dim strChars As String = ""         'this will be the return value, the chars in the lic plate

        Dim imgThreshColor As New Mat()

        listOfMatchingChars.Sort(Function(oneChar, otherChar) oneChar.boundingRect.X.CompareTo(otherChar.boundingRect.X))   'sort chars from left to right

        CvInvoke.CvtColor(imgThresh, imgThreshColor, ColorConversion.Gray2Bgr)

        For Each currentChar As PossibleChar In listOfMatchingChars                                 'for each char in plate
            CvInvoke.Rectangle(imgThreshColor, currentChar.boundingRect, SCALAR_GREEN, 2)           'draw green box around the char

            Dim imgROItoBeCloned As New Mat(imgThresh, currentChar.boundingRect)            'get ROI image of bounding rect

            Dim imgROI As Mat = imgROItoBeCloned.Clone()            'clone ROI image so we don't change original when we resize

            Dim imgROIResized As New Mat()

                    'resize image, this is necessary for char recognition
            CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_CHAR_IMAGE_WIDTH, RESIZED_CHAR_IMAGE_HEIGHT))

                    'declare a Matrix of the same dimensions as the Image we are adding to the data structure of training images
            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())

                    'declare a flattened (only 1 row) matrix of the same total size
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_CHAR_IMAGE_WIDTH * RESIZED_CHAR_IMAGE_HEIGHT)

            imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)       'convert Image to a Matrix of Singles with the same dimensions

            For intRow As Integer = 0 To RESIZED_CHAR_IMAGE_HEIGHT - 1          'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                For intCol As Integer = 0 To RESIZED_CHAR_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_CHAR_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next

            Dim sngCurrentChar As Single

            sngCurrentChar = kNearest.Predict(mtxTempReshaped)      'finally we can call Predict !!!

            strChars = strChars + Chr(Convert.ToInt32(sngCurrentChar))      'append current char to full string of chars
        Next

        If (frmMain.cbShowSteps.Checked = True) Then ' show steps '''''''''''''''''''''''''''''''''
            CvInvoke.Imshow("10", imgThreshColor)
        End If ' show steps '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        
        Return strChars         'return result
    End Function
    
End Module


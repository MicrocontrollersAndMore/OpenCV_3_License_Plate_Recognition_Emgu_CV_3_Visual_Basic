'frmMain.vb
'
'Emgu CV 3.0.0
'
'add the following components to your form:
'tableLayoutPanel (TableLayoutPanel)
'btnOpenFile (Button)
'lblChosenFile (Label)
'ibOriginal (ImageBox)
'txtInfo (TextBox)
'cbShowSteps (CheckBox)
'ofdOpenFile (OpenFileDialog)

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                 '
Imports Emgu.CV.CvEnum          'usual Emgu Cv imports
Imports Emgu.CV.Structure       '
Imports Emgu.CV.UI              '
Imports Emgu.CV.Util

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED As Single = 75       'these are for changing the proportion of image box to text box based on if we are showing steps or not
    Const TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED  As Single = 25

    Const IMAGE_BOX_PCT_SHOW_STEPS_CHECKED As Single = 55           'the idea is to show more of the text box if we are showing steps since there is more text to display
    Const TEXT_BOX_PCT_SHOW_STEPS_CHECKED As Single = 45

    Dim SCALAR_RED As New MCvScalar(0.0, 0.0, 255.0)
    Dim SCALAR_YELLOW As New MCvScalar(0.0, 255.0, 255.0)

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load  
        cbShowSteps_CheckedChanged(New Object, New EventArgs)                           'call check box event to update form based on check box initial state

        Dim blnKNNTrainingSuccessful As Boolean = loadKNNDataAndTrainKNN()              'attempt KNN training

        If (blnKNNTrainingSuccessful = False) Then                                              'if KNN training was not successful
            txtInfo.AppendText(vbCrLf + "error: KNN traning was not successful" + vbCrLf)       'show message on text box
            MsgBox("error: KNN traning was not successful")                                     'also show message box
            btnOpenFile.Enabled = False                                                         'disable open file button
            Return                                                                              'and bail
        End If
        
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub cbShowSteps_CheckedChanged(sender As Object, e As EventArgs) Handles cbShowSteps.CheckedChanged           'if show steps checkbox state changes,
        If (cbShowSteps.Checked = False) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED            'if showing steps, show more of the text box
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED
        ElseIf (cbShowSteps.Checked = True) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_CHECKED                'if not showing steps, show less of the text box
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_CHECKED
        End If
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenFile_Click(sender As Object, e As EventArgs) Handles btnOpenFile.Click  
        Dim imgOriginalScene As New Mat()                       'this is the original image scene

        Dim blnImageOpenedSuccessfully = openImageWithErrorHandling(imgOriginalScene)       'attempt to open image

        If (Not blnImageOpenedSuccessfully) Then                    'if image was not opened successfully
            ibOriginal.Image = Nothing                              'set the image box on the form to blank
            Return                                                  'and bail
        End If

        lblChosenFile.Text = ofdOpenFile.FileName                   'update label with file name

        CvInvoke.DestroyAllWindows()                        'close any windows that are open from previous button press

        ibOriginal.Image = imgOriginalScene                 'show original image on main form

        Dim listOfPossiblePlates As List(Of PossiblePlate) = DetectPlates.detectPlatesInScene(imgOriginalScene)     'detect plates
        
        listOfPossiblePlates = DetectChars.detectCharsInPlates(listOfPossiblePlates)                            'detect chars in plates

        If (listOfPossiblePlates Is Nothing) Then                                       'check if list of plates is null or zero
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        ElseIf (listOfPossiblePlates.Count = 0) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        Else
                                'if we get in here list of possible plates has at leat one plate

                                'sort the list of possible plates in DESCENDING order (most number of chars to least number of chars)
            listOfPossiblePlates.Sort(Function(onePlate, otherPlate) otherPlate.strChars.Length.CompareTo(onePlate.strChars.Length))

                                                                        'suppose the plate with the most recognized chars
            Dim licPlate As PossiblePlate = listOfPossiblePlates(0)     '(the first plate in sorted by string length descending order)
                                                                        'is the actual plate

            CvInvoke.Imshow("final imgPlate", licPlate.imgPlate)            'show the final color plate image 
            CvInvoke.Imshow("final imgThresh", licPlate.imgThresh)          'show the final thresh plate image
            
            If (licPlate.strChars.Length = 0) Then                          'if no chars are present in the lic plate,
                txtInfo.AppendText(vbCrLf + "no characters were detected" + licPlate.strChars + vbCrLf)     'update info text box
                Return                                                                                      'and return
            End If

            drawRedRectangleAroundPlate(imgOriginalScene, licPlate)                 'draw red rectangle around plate
            
            txtInfo.AppendText(vbCrLf + "license plate read from image = " + licPlate.strChars + vbCrLf)        'write license plate text to text box
            txtInfo.AppendText(vbCrLf + "----------------------------------------" + vbCrLf)
            
            writeLicensePlateCharsOnImage(imgOriginalScene, licPlate)                   'write license plate text on the image
            
            ibOriginal.Image = imgOriginalScene                                     'update image on main form
            
            CvInvoke.Imwrite("imgOriginalScene.png", imgOriginalScene)              'write image out to file
        End If
        
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function openImageWithErrorHandling(ByRef imgOriginalScene As Mat) As Boolean
        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> DialogResult.OK Or ofdOpenFile.FileName = "") Then          'if user did not choose anything
            lblChosenFile.Text = "file not chosen"                                      'update label
            Return False                                                                'and bail
        End If
        
        Try
            imgOriginalScene = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color)           'open image
        Catch ex As Exception                                                                       'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message                       'show error message on label
            Return False                                                                            'and exit function
        End Try

        If (imgOriginalScene Is Nothing) Then                                   'if image could not be opened
            lblChosenFile.Text = "unable to open image, image was null"         'show error message on label
            Return False                                                        'and exit function
        End If

        If (imgOriginalScene.IsEmpty()) Then                                    'if image opened as empty
            lblChosenFile.Text = "unable to open image, image was empty"        'show error message on label
            Return False                                                        'and exit function
        End If

        Return True
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub drawRedRectangleAroundPlate(imgOriginalScene As Mat, licPlate As PossiblePlate)
        Dim ptfRectPoints(4) As PointF                                          'declare array of 4 points, floating point type

        ptfRectPoints = licPlate.rrLocationOfPlateInScene.GetVertices()             'get 4 vertices of rotated rect

        Dim pt0 As New Point(CInt(ptfRectPoints(0).X), CInt(ptfRectPoints(0).Y))            'declare 4 points, integer type
        Dim pt1 As New Point(CInt(ptfRectPoints(1).X), CInt(ptfRectPoints(1).Y))
        Dim pt2 As New Point(CInt(ptfRectPoints(2).X), CInt(ptfRectPoints(2).Y))
        Dim pt3 As New Point(CInt(ptfRectPoints(3).X), CInt(ptfRectPoints(3).Y))

        CvInvoke.Line(imgOriginalScene, pt0, pt1, SCALAR_RED, 2)        'draw 4 red lines
        CvInvoke.Line(imgOriginalScene, pt1, pt2, SCALAR_RED, 2)
        CvInvoke.Line(imgOriginalScene, pt2, pt3, SCALAR_RED, 2)
        CvInvoke.Line(imgOriginalScene, pt3, pt0, SCALAR_RED, 2)
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub writeLicensePlateCharsOnImage(ByRef imgOriginalScene As Mat, licPlate As PossiblePlate)
        Dim ptCenterOfTextArea As New Point()                       'this will be the center of the area the text will be written to
        Dim ptLowerLeftTextOrigin As New Point()                    'this will be the bottom left of the area that the text will be written to

        Dim fontFace As FontFace = FontFace.HersheySimplex                  'choose a plain jane font
        Dim dblFontScale As Double = licPlate.imgPlate.Height / 30          'base font scale on height of plate area
        Dim intFontThickness As Integer = CInt(dblFontScale * 1.5)          'base font thickness on font scale
        Dim textSize As New Size()

                'to get the text size, we should use the OpenCV function getTextSize, but for some reason Emgu CV does not include this
                'we can instead estimate the test size based on the font scale, this will not be especially accurate but is good enough for our purposes here
        textSize.Width = CInt(dblFontScale * 18.5 * licPlate.strChars.Length)
        textSize.Height = CInt(dblFontScale * 25)
        
        ptCenterOfTextArea.X = CInt(licPlate.rrLocationOfPlateInScene.Center.X)         'the horizontal location of the text area is the same as the plate
        
        If (licPlate.rrLocationOfPlateInScene.Center.Y < (imgOriginalScene.Height * 0.75)) Then             'if the license plate is in the upper 3/4 of the image, we will write the chars in below the plate
            ptCenterOfTextArea.Y = CInt(licPlate.rrLocationOfPlateInScene.Center.Y + CInt(CDbl(licPlate.rrLocationOfPlateInScene.MinAreaRect.Height) * 1.6))
        Else                                'else if the license plate is in the lower 1/4 of the image, we will write the chars in above the plate
            ptCenterOfTextArea.Y = CInt(licPlate.rrLocationOfPlateInScene.Center.Y - CInt(CDbl(licPlate.rrLocationOfPlateInScene.MinAreaRect.Height) * 1.6))
        End If

        ptLowerLeftTextOrigin.X = CInt(ptCenterOfTextArea.X - (textSize.Width / 2))         'calculate the lower left origin of the text area
        ptLowerLeftTextOrigin.Y = CInt(ptCenterOfTextArea.Y + (textSize.Height / 2))        'based on the text area center, width, and height
        
        CvInvoke.PutText(imgOriginalScene, licPlate.strChars, ptLowerLeftTextOrigin, fontFace, dblFontScale, SCALAR_YELLOW, intFontThickness)       'write the text on the image
    End Sub
    
End Class






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

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED As Single = 75
    Const TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED  As Single = 25

    Const IMAGE_BOX_PCT_SHOW_STEPS_CHECKED As Single = 55
    Const TEXT_BOX_PCT_SHOW_STEPS_CHECKED As Single = 45

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        cbShowSteps_CheckedChanged(New Object, New EventArgs)
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub cbShowSteps_CheckedChanged(sender As Object, e As EventArgs) Handles cbShowSteps.CheckedChanged
        If (cbShowSteps.Checked = False) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_NOT_CHECKED
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_NOT_CHECKED
        ElseIf (cbShowSteps.Checked = True) Then
            tableLayoutPanel.RowStyles.Item(1).Height = IMAGE_BOX_PCT_SHOW_STEPS_CHECKED
            tableLayoutPanel.RowStyles.Item(2).Height = TEXT_BOX_PCT_SHOW_STEPS_CHECKED
        End If
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenFile_Click(sender As Object, e As EventArgs) Handles btnOpenFile.Click
        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> DialogResult.OK Or ofdOpenFile.FileName = "") Then
            lblChosenFile.Text = "file not chosen"
            Return
        End If

        Dim imgOriginalScene As Mat

        Try
            imgOriginalScene = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color)
        Catch ex As Exception
            lblChosenFile.Text = "unable to open image, error: " + ex.Message 
            Return
        End Try

        If (imgOriginalScene Is Nothing) Then
            lblChosenFile.Text = "unable to open image"
            Return
        End If

        If (imgOriginalScene.IsEmpty()) Then
            lblChosenFile.Text = "unable to open image"
            Return
        End If

        lblChosenFile.Text = ofdOpenFile.FileName

        CvInvoke.DestroyAllWindows()

        ibOriginal.Image = imgOriginalScene

        Dim listOfPossiblePlates As List(Of PossiblePlate) = DetectPlates.detectPlatesInScene(imgOriginalScene)

        Dim blnKNNTrainingSuccessful As Boolean = loadKNNDataAndTrainKNN()

        If (blnKNNTrainingSuccessful = False) Then
            txtInfo.AppendText(vbCrLf + "error: KNN traning was not successful" + vbCrLf)
            Return
        End If

        listOfPossiblePlates = DetectChars.detectCharsInPlates(listOfPossiblePlates)

        If (listOfPossiblePlates Is Nothing) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        ElseIf (listOfPossiblePlates.Count = 0) Then
            txtInfo.AppendText(vbCrLf + "no license plates were detected" + vbCrLf)
        Else
            listOfPossiblePlates.Sort(Function(onePlate, otherPlate) otherPlate.strChars.Length.CompareTo(onePlate.strChars.Length))

            Dim licPlate As PossiblePlate = listOfPossiblePlates(0)

            CvInvoke.Imshow("final imgPlate", licPlate.imgPlate)
            CvInvoke.Imshow("final imgThresh", licPlate.imgThresh)
            CvInvoke.Imwrite("imgThresh.png", licPlate.imgThresh)

            If (licPlate.strChars.Length = 0) Then
                txtInfo.AppendText(vbCrLf + "no characters were detected" + licPlate.strChars + vbCrLf)
                Return
            End If

            Dim p2fRectPoints(4) As PointF


            licPlate.rrLocationOfPlateInScene.points(p2fRectPoints)


            licPlate.rrLocationOfPlateInScene.








        End If
        
    End Sub

End Class

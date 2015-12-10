'PossiblePlate.vb
'
'Emgu CV 3.0.0

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class PossiblePlate

    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public imgPlate As Mat
    Public imgGrayscale As Mat
    Public imgThresh As Mat

    Public rrLocationOfPlateInScene As RotatedRect

    Public strChars As String

    ' constructor '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub New
                                'initialize values
        imgPlate = Nothing
        imgGrayscale = Nothing
        imgThresh = Nothing

        strChars = ""
    End Sub

End Class

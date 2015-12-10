'DetectPlates.vb
'
'Emgu CV 3.0.0

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                     '
Imports Emgu.CV.CvEnum              'Emgu Cv imports
Imports Emgu.CV.Structure           '
Imports Emgu.CV.UI                  '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectPlates

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const PLATE_WIDTH_PADDING_FACTOR As Double = 1.5
    Const PLATE_HEIGHT_PADDING_FACTOR As Double = 1.5

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectPlatesInScene(imgOriginalScene As Mat) As List(Of PossiblePlate)
        Dim listOfPossiblePlates As List(Of PossiblePlate) = New List(Of PossiblePlate)         'this will be the return value




        Return listOfPossiblePlates
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInScene(imgThresh As Mat) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)()      'this is the return value




        Return listOfPossibleChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function extractPlate(imgOriginal As Mat, listOfMatchingChars As List(Of PossibleChar)) As PossiblePlate
        Dim possiblePlate As PossiblePlate = New PossiblePlate          'this will be the return value






        Return possiblePlate
    End Function

End Module

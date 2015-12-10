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

Imports System.Xml
Imports System.Xml.Serialization    'these imports are for reading Matrix objects from file
Imports System.IO

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Module DetectChars
    
    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                                'constants for checkIfPossibleChar, this checks one possible char only (does not compare to another char)
    Const MIN_PIXEL_WIDTH As Long = 2
    Const MIN_PIXEL_HEIGHT As Long = 8

    Const MIN_ASPECT_RATIO As Double = 0.25
    Const MAX_ASPECT_RATIO As Double = 1.0

    Const MIN_PIXEL_AREA As Long = 20

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
    
                                'variables
    Dim kNearest As KNearest

    Const MIN_CONTOUR_AREA As Integer = 100

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function detectCharsInPlates(listOfPossiblePlates As List(Of PossiblePlate)) As List(Of PossiblePlate)

        




        Return listOfPossiblePlates
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findPossibleCharsInPlate(imgGrayscale As Image(Of Gray, Byte), imgThresh As Image(Of Gray, Byte)) As List(Of PossibleChar)
        Dim listOfPossibleChars As List(Of PossibleChar) = New List(Of PossibleChar)        'this will be the return value







        Return listOfPossibleChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function checkIfPossibleChar(possibleChar As PossibleChar) As Boolean
        If (possibleChar.boundingRect.Width > MIN_PIXEL_WIDTH And possibleChar.boundingRect.Height > MIN_PIXEL_HEIGHT And _
            MIN_ASPECT_RATIO < possibleChar.dblAspectRatio And possibleChar.dblAspectRatio < MAX_ASPECT_RATIO And _
            possibleChar.lngArea > MIN_PIXEL_AREA) Then
            Return True
        Else
            Return False
        End If
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfListsOfMatchingChars(listOfPossibleChars As List(Of PossibleChar)) As List(Of List(Of PossibleChar))
        Dim listOfListsOfMatchingChars As List(Of List(Of PossibleChar)) = New List(Of List(Of PossibleChar))       'this will be the return value







        Return listOfListsOfMatchingChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function findListOfMatchingChars(possibleChar As PossibleChar, listOfChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingChars As List(Of PossibleChar) = New List(Of PossibleChar)            'this will be the return value








        Return listOfMatchingChars
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function distanceBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim lngX As Long = Math.Abs(firstChar.lngCenterX - secondChar.lngCenterX)
        Dim lngY As Long = Math.Abs(firstChar.lngCenterY - secondChar.lngCenterY)

        Return Math.Sqrt((lngX ^ 2) + (lngY ^ 2))
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function angleBetweenChars(firstChar As PossibleChar, secondChar As PossibleChar) As Double
        Dim dblAdj As Double = CDbl(Math.Abs(firstChar.lngCenterX - secondChar.lngCenterX))
        Dim dblOpp As Double = CDbl(Math.Abs(firstChar.lngCenterY - secondChar.lngCenterY))

        Dim dblAngleInRad As Double = Math.Atan(dblOpp / dblAdj) 
        
        Dim dblAngleInDeg As Double = dblAngleInRad * (180.0 / Math.PI)

        Return dblAngleInDeg
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function removeInnerOverlappingChars(listOfMatchingChars As List(Of PossibleChar)) As List(Of PossibleChar)
        Dim listOfMatchingCharsWithInnerCharRemoved As List(Of PossibleChar) = New List(Of PossibleChar)(listOfMatchingChars)







        Return listOfMatchingCharsWithInnerCharRemoved
    End Function

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function recognizeCharsInPlate(imgThresh As Image(Of Gray, Byte), listOfMatchingChars As List(Of PossibleChar)) As String
        Dim strChars As String = ""         'this will be the return value, the chars in the lic plate






        Return strChars
    End Function
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Function loadKNNDataAndTrainKNN() As Boolean




        Return True
    End Function

End Module

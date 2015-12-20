<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.tableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
        Me.btnOpenFile = New System.Windows.Forms.Button()
        Me.lblChosenFile = New System.Windows.Forms.Label()
        Me.cbShowSteps = New System.Windows.Forms.CheckBox()
        Me.ibOriginal = New Emgu.CV.UI.ImageBox()
        Me.txtInfo = New System.Windows.Forms.TextBox()
        Me.ofdOpenFile = New System.Windows.Forms.OpenFileDialog()
        Me.tableLayoutPanel.SuspendLayout
        CType(Me.ibOriginal,System.ComponentModel.ISupportInitialize).BeginInit
        Me.SuspendLayout
        '
        'tableLayoutPanel
        '
        Me.tableLayoutPanel.ColumnCount = 3
        Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100!))
        Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tableLayoutPanel.Controls.Add(Me.btnOpenFile, 0, 0)
        Me.tableLayoutPanel.Controls.Add(Me.lblChosenFile, 1, 0)
        Me.tableLayoutPanel.Controls.Add(Me.cbShowSteps, 2, 0)
        Me.tableLayoutPanel.Controls.Add(Me.ibOriginal, 0, 1)
        Me.tableLayoutPanel.Controls.Add(Me.txtInfo, 0, 2)
        Me.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tableLayoutPanel.Location = New System.Drawing.Point(0, 0)
        Me.tableLayoutPanel.Name = "tableLayoutPanel"
        Me.tableLayoutPanel.RowCount = 3
        Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75!))
        Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25!))
        Me.tableLayoutPanel.Size = New System.Drawing.Size(1171, 782)
        Me.tableLayoutPanel.TabIndex = 0
        '
        'btnOpenFile
        '
        Me.btnOpenFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right),System.Windows.Forms.AnchorStyles)
        Me.btnOpenFile.AutoSize = true
        Me.btnOpenFile.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnOpenFile.Font = New System.Drawing.Font("Microsoft Sans Serif", 12!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0,Byte))
        Me.btnOpenFile.Location = New System.Drawing.Point(3, 3)
        Me.btnOpenFile.Name = "btnOpenFile"
        Me.btnOpenFile.Size = New System.Drawing.Size(107, 35)
        Me.btnOpenFile.TabIndex = 0
        Me.btnOpenFile.Text = "Open File"
        Me.btnOpenFile.UseVisualStyleBackColor = true
        '
        'lblChosenFile
        '
        Me.lblChosenFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right),System.Windows.Forms.AnchorStyles)
        Me.lblChosenFile.AutoSize = true
        Me.lblChosenFile.Font = New System.Drawing.Font("Microsoft Sans Serif", 12!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0,Byte))
        Me.lblChosenFile.Location = New System.Drawing.Point(116, 8)
        Me.lblChosenFile.Name = "lblChosenFile"
        Me.lblChosenFile.Size = New System.Drawing.Size(906, 25)
        Me.lblChosenFile.TabIndex = 1
        Me.lblChosenFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'cbShowSteps
        '
        Me.cbShowSteps.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right),System.Windows.Forms.AnchorStyles)
        Me.cbShowSteps.AutoSize = true
        Me.cbShowSteps.Font = New System.Drawing.Font("Microsoft Sans Serif", 12!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0,Byte))
        Me.cbShowSteps.Location = New System.Drawing.Point(1028, 6)
        Me.cbShowSteps.Name = "cbShowSteps"
        Me.cbShowSteps.Size = New System.Drawing.Size(140, 29)
        Me.cbShowSteps.TabIndex = 2
        Me.cbShowSteps.Text = "Show Steps"
        Me.cbShowSteps.UseVisualStyleBackColor = true
        '
        'ibOriginal
        '
        Me.tableLayoutPanel.SetColumnSpan(Me.ibOriginal, 3)
        Me.ibOriginal.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ibOriginal.Enabled = false
        Me.ibOriginal.Location = New System.Drawing.Point(3, 44)
        Me.ibOriginal.Name = "ibOriginal"
        Me.ibOriginal.Size = New System.Drawing.Size(1165, 549)
        Me.ibOriginal.TabIndex = 2
        Me.ibOriginal.TabStop = false
        '
        'txtInfo
        '
        Me.tableLayoutPanel.SetColumnSpan(Me.txtInfo, 3)
        Me.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtInfo.Location = New System.Drawing.Point(3, 599)
        Me.txtInfo.Multiline = true
        Me.txtInfo.Name = "txtInfo"
        Me.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtInfo.Size = New System.Drawing.Size(1165, 180)
        Me.txtInfo.TabIndex = 3
        Me.txtInfo.WordWrap = false
        '
        'ofdOpenFile
        '
        Me.ofdOpenFile.FileName = "OpenFileDialog1"
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8!, 16!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1171, 782)
        Me.Controls.Add(Me.tableLayoutPanel)
        Me.Name = "frmMain"
        Me.Text = "Form1"
        Me.tableLayoutPanel.ResumeLayout(false)
        Me.tableLayoutPanel.PerformLayout
        CType(Me.ibOriginal,System.ComponentModel.ISupportInitialize).EndInit
        Me.ResumeLayout(false)

End Sub

    Friend WithEvents tableLayoutPanel As TableLayoutPanel
    Friend WithEvents btnOpenFile As Button
    Friend WithEvents lblChosenFile As Label
    Friend WithEvents cbShowSteps As CheckBox
    Friend WithEvents ibOriginal As Emgu.CV.UI.ImageBox
    Friend WithEvents txtInfo As TextBox
    Friend WithEvents ofdOpenFile As OpenFileDialog
End Class

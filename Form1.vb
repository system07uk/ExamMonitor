Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Timers
Imports AForge.Video
Imports AForge.Video.DirectShow
Imports Newtonsoft.Json

Public Class Form1
    Dim StdNo As String, StdName As String
    Dim sHostName As String, sUserName As String
    Private lastTaskbarTitles As New List(Of String)
    Private newTitles As New List(Of String)
    Private removedTitles As New List(Of String)
    Public header As String
    Public loop_no, loop_no2 As Long

    Private videoSource As VideoCaptureDevice
    Private webcamImage As Bitmap

    ' ----- config.txt에서 읽어올 설정 -----
    Public webhook_url As String = ""
    Public timerInterval As Integer = 30000
    Public useWebcam As Boolean = True

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' ---------- config.txt 읽기 ----------
        Dim configPath As String = Path.Combine(Application.StartupPath, "config.txt")
        If Not File.Exists(configPath) Then
            MsgBox("config.txt 파일이 없습니다!" & vbCrLf & "ExamMonitor.exe와 같은 폴더에 config.txt를 넣어주세요.", MsgBoxStyle.Critical)
            End
        End If

        For Each line In File.ReadAllLines(configPath)
            Dim trimmed = line.Trim()
            If trimmed.StartsWith("webhook_url=") Then webhook_url = trimmed.Substring(12).Trim()
            If trimmed.StartsWith("timer_interval=") Then timerInterval = Integer.Parse(trimmed.Substring(15).Trim())
            If trimmed.StartsWith("use_webcam=") Then useWebcam = (trimmed.Substring(11).Trim().ToLower() = "true")
        Next

        If String.IsNullOrEmpty(webhook_url) Then
            MsgBox("config.txt에 webhook_url이 없습니다!", MsgBoxStyle.Critical)
            End
        End If

        ' ---------- 초기 UI (OFF 상태) ----------
        Label4.BackColor = Color.Red
        Label4.Text = "OFF"
        Check_cam.Checked = useWebcam
        Check_cam.Enabled = False   ' 학생이 건드리지 못하게

        ' 웹캠 장치 미리 준비 (버벅거림 방지)
        Dim videoDevices As New FilterInfoCollection(FilterCategory.VideoInputDevice)
        If videoDevices.Count > 0 Then
            videoSource = New VideoCaptureDevice(videoDevices(0).MonikerString)
            AddHandler videoSource.NewFrame, AddressOf Video_NewFrame
            ' 여기서는 Start() 하지 않음 → Button1에서 시작
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TextBox_StuNo.Text.Trim = "" Or TextBox_StuName.Text.Trim = "" Then
            MsgBox("학번과 이름을 입력하세요!", MsgBoxStyle.Exclamation)
            Exit Sub
        End If

        ' 학번/이름 저장
        StdNo = TextBox_StuNo.Text.Trim
        StdName = TextBox_StuName.Text.Trim

        ' UI 변경
        Button1.Visible = False
        Label4.BackColor = Color.FromArgb(100, 255, 100)
        Label4.Text = "ON - 0"
        TextBox_StuNo.Enabled = False
        TextBox_StuName.Enabled = False

        ' 헤더 생성
        sHostName = Environ$("computername")
        sUserName = Environ$("username")
        Dim myip As String = IPtest()
        header = $"{StdNo}: {StdName}: {myip}: {sHostName}: {sUserName}"

        ' 웹캠 시작
        If videoSource IsNot Nothing AndAlso Not videoSource.IsRunning Then
            videoSource.Start()
        End If

        ' 감독 시작
        InitializeTaskbarMonitoring()
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ' 웹캠 정지
        If videoSource IsNot Nothing AndAlso videoSource.IsRunning Then
            videoSource.SignalToStop()
            videoSource.WaitForStop()
        End If

        ' ----- 여기부터 추가: 종료 메시지 전송 -----
        Try
            ' 마지막 스크린샷 + 웹캠 캡처 (비동기 기다리지 않고 바로 동기식으로)
            Dim lastScreen As String = CaptureScreen()
            Dim lastWebcam As String = If(useWebcam, CaptureWebcam(), "")

            Using client As New HttpClient()
                Dim endPayload = New With {
                .username = "시험 감독 봇",
                .content = $"**{header}** 시험 종료 (프로그램 닫힘)",
                .embeds = New Object() {New With {
                    .description = "학생이 프로그램을 정상적으로 종료했습니다." & vbCrLf &
                                   "마지막 화면과 얼굴 사진을 확인하세요.",
                    .color = 3447003, ' 파란색
                    .timestamp = DateTime.UtcNow.ToString("o")
                }}
            }

                Dim multipart As New MultipartFormDataContent()
                multipart.Add(New StringContent(JsonConvert.SerializeObject(endPayload), Encoding.UTF8, "application/json"), "payload_json")

                If Not String.IsNullOrEmpty(lastScreen) Then
                    multipart.Add(New ByteArrayContent(File.ReadAllBytes(lastScreen)), "file", "최종_화면.png")
                    File.Delete(lastScreen)
                End If

                If Not String.IsNullOrEmpty(lastWebcam) Then
                    multipart.Add(New ByteArrayContent(File.ReadAllBytes(lastWebcam)), "file2", "최종_웹캠.png")
                    File.Delete(lastWebcam)
                End If

                ' 동기식 전송 (폼 닫히기 전에 반드시 보내기)
                Dim response = client.PostAsync(webhook_url, multipart).Result
                ' 결과는 무시 (네트워크 끊겨도 어차피 종료니까)
            End Using
        Catch
            ' 네트워크 오류 무시 (이미 종료 중이니까)
        End Try
        ' ----- 추가 끝 -----
    End Sub

    Private Sub Video_NewFrame(sender As Object, eventArgs As NewFrameEventArgs)
        webcamImage = DirectCast(eventArgs.Frame.Clone(), Bitmap)
    End Sub

    Public Function IPtest() As String
        Try
            Dim host = Dns.GetHostName()
            Return Dns.GetHostEntry(host).AddressList.FirstOrDefault(Function(a) a.AddressFamily = Net.Sockets.AddressFamily.InterNetwork)?.ToString()
        Catch
            Return "0.0.0.0"
        End Try
    End Function

    Public Sub Loop_update_mail(add_no As Long)
        If Label4.InvokeRequired Then
            Label4.Invoke(New Action(Of Long)(AddressOf Loop_update_mail), add_no)
        Else
            loop_no += add_no
            Label4.Text = "ON - " & loop_no & " " & loop_no2
        End If
    End Sub
    Public Sub Loop_update_task(add_no As Long)
        If Label4.InvokeRequired Then
            Label4.Invoke(New Action(Of Long)(AddressOf Loop_update_task), add_no)
        Else
            loop_no2 += add_no
            Label4.Text = "ON - " & loop_no & " " & loop_no2
        End If
    End Sub

    Private Sub InitializeTaskbarMonitoring()
        SendInitialTaskbarTitles()
        Dim timer As New Timers.Timer(timerInterval)
        AddHandler timer.Elapsed, AddressOf OnTimedEvent
        timer.AutoReset = True
        timer.Enabled = True
    End Sub

    Private Sub SendInitialTaskbarTitles()
        lastTaskbarTitles = GetTaskbarTitles()
        SendToDiscord(New List(Of String), New List(Of String), lastTaskbarTitles)
    End Sub

    Private Sub OnTimedEvent(source As Object, e As ElapsedEventArgs)
        Dim current = GetTaskbarTitles()
        Dim added = current.Except(lastTaskbarTitles).ToList()
        Dim removed = lastTaskbarTitles.Except(current).ToList()

        If added.Any() Or removed.Any() Then
            newTitles.AddRange(added)
            removedTitles.AddRange(removed)
            lastTaskbarTitles = current
        End If

        SendToDiscord(newTitles, removedTitles, lastTaskbarTitles)
        newTitles.Clear()
        removedTitles.Clear()
    End Sub

    Private Function GetTaskbarTitles() As List(Of String)
        Dim list As New List(Of String)
        For Each p As Process In Process.GetProcesses()
            Try
                If Not String.IsNullOrEmpty(p.MainWindowTitle) Then
                    list.Add(p.MainWindowTitle)
                    Loop_update_task(1)
                End If
            Catch : End Try
        Next
        Return list
    End Function

    Private Async Sub SendToDiscord(newTitles As List(Of String), removedTitles As List(Of String), currentTitles As List(Of String))
        Try
            Using client As New HttpClient()
                Dim desc As String = $"**추가**{If(newTitles.Any, vbCrLf & "• " & String.Join(vbCrLf & "• ", newTitles), ": 없음")}" & vbCrLf &
                                     $"**제거**{If(removedTitles.Any, vbCrLf & "• " & String.Join(vbCrLf & "• ", removedTitles), ": 없음")}" & vbCrLf & vbCrLf &
                                     $"**현재 실행 중**{If(currentTitles.Any, vbCrLf & "• " & String.Join(vbCrLf & "• ", currentTitles.Take(15)), ": 없음")}"

                Dim payload = New With {
                    .username = "시험 감독 봇",
                    .content = $"**{header}** | {DateTime.Now:HH:mm:ss}",
                    .embeds = New Object() {New With {
                        .description = desc,
                        .color = If(newTitles.Any Or removedTitles.Any, 16711680, 65280),
                        .timestamp = DateTime.UtcNow.ToString("o")
                    }}
                }

                Dim multipart As New MultipartFormDataContent()
                multipart.Add(New StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"), "payload_json")

                ' 스크린샷
                Dim screenFilePath As String = CaptureScreen()
                If Not String.IsNullOrEmpty(screenFilePath) Then
                    multipart.Add(New ByteArrayContent(File.ReadAllBytes(screenFilePath)), "file", "screen.png")
                    File.Delete(screenFilePath)
                End If

                ' 웹캠
                If useWebcam Then
                    Dim webcamFilePath As String = CaptureWebcam()
                    If Not String.IsNullOrEmpty(webcamFilePath) Then
                        multipart.Add(New ByteArrayContent(File.ReadAllBytes(webcamFilePath)), "file2", "webcam.png")
                        File.Delete(webcamFilePath)
                    End If
                End If

                Dim resp = Await client.PostAsync(webhook_url, multipart)
                If resp.IsSuccessStatusCode Then Loop_update_mail(1)
            End Using
        Catch
            ' 네트워크 문제라도 프로그램은 계속 돈다
        End Try
    End Sub

    Private Function CaptureScreen() As String
        Try
            Dim bounds = Screen.PrimaryScreen.Bounds
            Using bmp As New Bitmap(bounds.Width, bounds.Height)
                Using g = Graphics.FromImage(bmp)
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size)
                End Using
                Dim screenFilePath As String = Path.Combine(Path.GetTempPath(), "scr_" & DateTime.Now.Ticks & ".png")
                bmp.Save(screenFilePath, ImageFormat.Png)
                Return screenFilePath
            End Using
        Catch
            Return String.Empty
        End Try
    End Function

    Private Function CaptureWebcam() As String
        Try
            If webcamImage IsNot Nothing Then
                Dim webcamFilePath As String = Path.Combine(Path.GetTempPath(), "cam_" & DateTime.Now.Ticks & ".png")
                webcamImage.Save(webcamFilePath, ImageFormat.Png)
                Return webcamFilePath
            End If
        Catch
        End Try
        Return String.Empty
    End Function
End Class
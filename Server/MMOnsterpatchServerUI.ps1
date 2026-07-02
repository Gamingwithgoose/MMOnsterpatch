Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$script:serverProcess = $null
$script:stopping = $false
$script:root = Split-Path -Parent $MyInvocation.MyCommand.Path

$form = New-Object System.Windows.Forms.Form
$form.Text = 'MMOnsterpatch Official Server'
$form.Size = New-Object System.Drawing.Size(760, 520)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(245, 235, 201)
$form.Font = New-Object System.Drawing.Font('Segoe UI', 9)

$btnStart = New-Object System.Windows.Forms.Button
$btnStart.Text = 'Start Server'
$btnStart.Location = New-Object System.Drawing.Point(12, 12)
$btnStart.Size = New-Object System.Drawing.Size(150, 40)
$btnStart.BackColor = [System.Drawing.Color]::FromArgb(42, 170, 70)
$btnStart.ForeColor = [System.Drawing.Color]::White
$btnStart.FlatStyle = 'Flat'
$form.Controls.Add($btnStart)

$chkRestart = New-Object System.Windows.Forms.CheckBox
$chkRestart.Text = 'Auto restart on close/crash'
$chkRestart.Location = New-Object System.Drawing.Point(180, 22)
$chkRestart.Size = New-Object System.Drawing.Size(230, 24)
$chkRestart.Checked = $false
$form.Controls.Add($chkRestart)

$lblCmd = New-Object System.Windows.Forms.Label
$lblCmd.Text = 'Admin command:'
$lblCmd.Location = New-Object System.Drawing.Point(12, 64)
$lblCmd.Size = New-Object System.Drawing.Size(120, 24)
$form.Controls.Add($lblCmd)

$txtCmd = New-Object System.Windows.Forms.TextBox
$txtCmd.Location = New-Object System.Drawing.Point(130, 62)
$txtCmd.Size = New-Object System.Drawing.Size(470, 24)
$txtCmd.Anchor = 'Top,Left,Right'
$form.Controls.Add($txtCmd)

$btnSend = New-Object System.Windows.Forms.Button
$btnSend.Text = 'Send'
$btnSend.Location = New-Object System.Drawing.Point(610, 60)
$btnSend.Size = New-Object System.Drawing.Size(120, 28)
$btnSend.Anchor = 'Top,Right'
$form.Controls.Add($btnSend)

$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Location = New-Object System.Drawing.Point(12, 100)
$txtLog.Size = New-Object System.Drawing.Size(718, 365)
$txtLog.Anchor = 'Top,Bottom,Left,Right'
$txtLog.Multiline = $true
$txtLog.ScrollBars = 'Vertical'
$txtLog.ReadOnly = $true
$txtLog.BackColor = [System.Drawing.Color]::FromArgb(30, 25, 20)
$txtLog.ForeColor = [System.Drawing.Color]::FromArgb(230, 230, 230)
$txtLog.Font = New-Object System.Drawing.Font('Consolas', 9)
$form.Controls.Add($txtLog)

function Invoke-UiSafe([ScriptBlock]$action) {
    try {
        if ($form.IsDisposed) { return }
        if ($form.IsHandleCreated -and $form.InvokeRequired) {
            $form.BeginInvoke([Action]{ & $action }) | Out-Null
        } else {
            & $action
        }
    } catch {
        # Ignore late UI updates during startup/shutdown races.
    }
}

function Append-Log([string]$line) {
    Invoke-UiSafe { $txtLog.AppendText($line + [Environment]::NewLine) }
}

function Set-RunningUi([bool]$running) {
    Invoke-UiSafe {
        if ($running) {
            $btnStart.Text = 'Stop Server'
            $btnStart.BackColor = [System.Drawing.Color]::FromArgb(190, 45, 45)
        } else {
            $btnStart.Text = 'Start Server'
            $btnStart.BackColor = [System.Drawing.Color]::FromArgb(42, 170, 70)
        }
    }
}

function Start-Server {
    if ($script:serverProcess -ne $null -and -not $script:serverProcess.HasExited) { return }
    $script:stopping = $false
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'python'
    $psi.Arguments = '-u "' + (Join-Path $script:root 'MMOnsterpatchServer.py') + '" --config "' + (Join-Path $script:root 'configs\worldserver.ini') + '"'
    $psi.WorkingDirectory = $script:root
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $psi
    $p.EnableRaisingEvents = $true
    Register-ObjectEvent -InputObject $p -EventName OutputDataReceived -Action { if ($EventArgs.Data) { Append-Log $EventArgs.Data } } | Out-Null
    Register-ObjectEvent -InputObject $p -EventName ErrorDataReceived -Action { if ($EventArgs.Data) { Append-Log ('ERR: ' + $EventArgs.Data) } } | Out-Null
    Register-ObjectEvent -InputObject $p -EventName Exited -Action {
        Append-Log 'Server process exited.'
        Set-RunningUi $false
        if ($chkRestart.Checked -and -not $script:stopping) {
            Start-Sleep -Seconds 2
            Append-Log 'Auto restart enabled; restarting server...'
            Invoke-UiSafe { Start-Server }
        }
    } | Out-Null
    if ($p.Start()) {
        $script:serverProcess = $p
        $p.BeginOutputReadLine()
        $p.BeginErrorReadLine()
        Set-RunningUi $true
        Append-Log 'Server started.'
    } else {
        Append-Log 'Failed to start server.'
    }
}

function Stop-Server {
    $script:stopping = $true
    if ($script:serverProcess -ne $null -and -not $script:serverProcess.HasExited) {
        try {
            $script:serverProcess.StandardInput.WriteLine('/shutdown')
            Start-Sleep -Milliseconds 300
        } catch {}
        try { $script:serverProcess.Kill() } catch {}
    }
    Set-RunningUi $false
    Append-Log 'Server stopped.'
}

$btnStart.Add_Click({
    if ($script:serverProcess -ne $null -and -not $script:serverProcess.HasExited) { Stop-Server } else { Start-Server }
})

$sendAction = {
    $cmd = $txtCmd.Text.Trim()
    if ($cmd.Length -eq 0) { return }
    if ($script:serverProcess -eq $null -or $script:serverProcess.HasExited) {
        Append-Log 'Server is not running.'
        return
    }
    try {
        $script:serverProcess.StandardInput.WriteLine($cmd)
        Append-Log ('> ' + $cmd)
        $txtCmd.Clear()
    } catch {
        Append-Log ('Failed to send command: ' + $_.Exception.Message)
    }
}
$btnSend.Add_Click($sendAction)
$txtCmd.Add_KeyDown({ if ($_.KeyCode -eq 'Enter') { & $sendAction; $_.SuppressKeyPress = $true } })

$form.Add_FormClosing({ if ($script:serverProcess -ne $null -and -not $script:serverProcess.HasExited) { Stop-Server } })
Append-Log 'Ready. Commands: /system message, /givemon Player#1234|ALL MonName [level] [Shiny], /giveitem Player#1234|ALL ItemName [amount], /givesats Player#1234|ALL amount, /ban, /unban, /banlist.'
[void]$form.ShowDialog()

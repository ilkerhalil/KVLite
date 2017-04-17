<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PommaLabs.KVLite.Examples.WebForms.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Label ID="lblDateTime" runat="server" Text="..."></asp:Label>
        </div>
        <p>
            <asp:Button ID="btnRefresh" runat="server" OnClick="btnRefresh_Click" Text="Button" />
        </p>
    </form>
</body>
</html>

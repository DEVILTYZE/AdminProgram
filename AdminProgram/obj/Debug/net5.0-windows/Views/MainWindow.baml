   M S B A M L   `   `   `  ����G  CAdminProgram, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:AdminProgramAdminProgram  5   	   A%clr-namespace:AdminProgram.ViewModelsAdminProgram.ViewModels  5   	   ;"clr-namespace:AdminProgram.HelpersAdminProgram.Helpers  5   	   #    AdminProgram.Views.MainWindow   5   	   W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ,localclr-namespace:AdminProgram     5   	   <
viewModels%clr-namespace:AdminProgram.ViewModels     5   	   6helpers"clr-namespace:AdminProgram.Helpers     5   	   -   5          � Title$  
MainWindow��5
   	   $	��550��6   $
��1000��6,   $	��800��6<   $
��1500��6I    � DataContext 5      +   %AdminProgram.ViewModels.HostViewModel  5   
    � 	Resources 5       �� 
TargetType 
  Window �� BasedOn �� Property �� Value  
SmokeWhite��%�   '�       7   '�� "     7   7  �� 5   
   " �6   8   6"   �� 5   
   " ��6   8   .�5      ��  � ��$ 
20 1 20 10q�6'   �� 5       � ��$ 1q�60   �� 5       � ��8  6,   5      5      .��5      � $
��1000��6    � RowDefinitions 5   
   �� 5      $
��0.1* �6   $��75��6*   �� 5       � ColumnDefinitions 5   
   �� 5      �� 5      �� 5       �� 5!      .+�5#   
   �� -   5        	 ��Name$	 AddHostButton��5#      $��Добавить хост��5$      5%      �� 5&   
   -   5        $	 
ScanButton��5'      $��0��5&      $��1��6   #��[B��5(      ��5)      �� *ScanTasks.IsAlive+  ScanButtonText
 �� 	Converter#
 [ {�5+      �� *ScanTasks.IsAlive+  OppositeBool#
 [ 5-      �� 5.   
   -   5        $	 RefreshAllButton��5/      $��0��5.      $��2��6   #��[B��50      ��51      �� *RefreshTasks.IsAlive+  RefreshButtonText#
 [ {�53      �� *RefreshTasks.IsAlive+#
 [ 55      -   'AdminProgram.Helpers.TextBoxPlaceholder  56   
   -   5          Name$ 	SearchBox��57   %   -   'AdminProgram.Helpers.TextBoxPlaceholder   KeyDown$ SearchBox_OnKeyDown��5;   %   $��0��56   %   $��3��62   $��75��58   %   $	��400��59   %      Placeholder$ 
Поиск��5:   %   5;   %   �� 5F   
   -   5         ��Name$ HostsBox��5F      $��1��5G      $��0��6   $��3��6/   #��[  5J      $��10q�5L      ��5H      �� *Hosts+ �� SelectedItem 5I      �� *SelectedHost+��5M      �� 5N      .R�5O      ��  �� Orientation$ 
Horizontal=�6!   .+�5P      ��   	HostPanel#��[ 6$   ��6G   ��  �� Path$
 Name��� 5Q      #��[ 6$   ��6G   �� $ 	IpAddress��� 5R      #��[ 6$   ��6G   �� $ 
MacAddress�A� 5S      #��[BA�6"   ��5T   "   �� *Status+  EllipseColor#
 [ �� 5U      #��[ 6$   ��5V   $   �� *Status+  
StatusText#
 [ 5W      5X      5Z      �� 5[   
   -   5         ��Name$ 
RightPanel��5\      $��1��5[      $��3��6"     
RightPanel#��[ 5]      $	��400��5^      $	��100��6%   .+�5_      �� #��[ 6   ��5`      �� $ SelectedHost.Name� �� StringFormat$ Имя: {0}���� 5a      #��[ 6   ��5b      �� $ SelectedHost.IpAddress�$ IP-адрес: {0}���� 5c      #��[ 6   ��5d      �� $ SelectedHost.MacAddress�$ MAC-адрес: {0}���� 5e      $ 
Horizontal=�6   .+�5f      �� #��[ 6   $��Статус:��6?   A� 5g      #��[BA�6   ��5h      �� *SelectedHost.Status+#
 [ �� 5j      #��[ 6   ��5k      �� *SelectedHost.Status+#
 [ 5m      �� 5n      -   5        #��[B��5n      ��5o      �� *SelectedHost.Status+ 	 PowerOnButtonText#
 [	 {�5q      �� *SelectedHost.Status+ 
 StatusBoolPower#
 [
 5s      �� 5t      -	   5        #��[B��5t      $#��Обновить статус��5u      {�5v      �� *SelectedHost.Status+#
 [
 5x      �� 5y      -
   5        $	 RemoveHostButton��5y      $��Удалить хост��6/   6F   �� 5z      {�6   �� *SelectedHost.Status+  
StatusBool#
 [ .+�5~      �� -   5        $	 RemoteButton��5~      #��[B��5      $-��'Удалённое управление��5�      5�      �� 5�        TextBox#��[ 6   $	��600��5�      ��5�      �� *TransferMessage+5�      �� 5�      -   5        ��5�      �� *SelectedHost.IsTransfers+  TransferButtonText#
 [ 5�      5�      5�      5�      5�      
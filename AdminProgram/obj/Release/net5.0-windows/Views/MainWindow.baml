   M S B A M L   `   `   `  ���� G  CAdminProgram, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullA%clr-namespace:AdminProgram.ViewModelsAdminProgram.ViewModels  #    AdminProgram.Views.MainWindow   W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     <
viewModels%clr-namespace:AdminProgram.ViewModels     -     � Title$  Admin Program��$	��650��$
��1275��$	��800��$
��1500�� � DataContext +   %AdminProgram.ViewModels.HostViewModel   � 	Resources  �� 
TargetType 
  Window �� BasedOn �� Property �� Value��%�   '�       7   '��      7   �� " �8   �� " ��8   .���  � ��$ 
20 1 20 10q���  � ��$ 1q���  � ��
 �����.���� .@��� ����   	FontSmall#��[ Список хостов��� $��Stretch=� � RowDefinitions �� $��75 ���  � ColumnDefinitions �� �� �� �� .+��� -   $��Добавить хост���� -   	 ��Name$	 
ScanButton��$��0��$��1��#��[B������ *ScanTasks.IsAlive+  ScanButtonText
 �� 	Converter#
 [ {��� *ScanTasks.IsAlive+  OppositeBool#
 [ �� -   $	 RefreshAllButton��$��0��$��2��#��[B������ *RefreshTasks.IsAlive+  RefreshButtonText#
 [ {��� *RefreshTasks.IsAlive+#
 [ ]� $��0��$��3��.+��� $��Поиск:��$��Center=�$��Center=�$
��Left=� �� Target %  System.Windows.Markup.Reference  *	SearchBox+�� -    ��Name$ 	SearchBox��  	SearchBox#��[  � Cursor$ IBeam��$��Stretch=��� -    ��Name$ HostsBox��$��1��$��0��$��3��#��[  $��10q����� *Hosts+ �� SelectedItem �� *SelectedHost+���� .R���  �� Orientation$ 
Horizontal=�.+���   	HostPanel#��[ ����  �� Path$
 Name��� #��[ ���� $ 	IpAddress��� #��[ ���� $ 
MacAddress�A� #��[BA����� *Status+  EllipseColor#
 [ �� #��[ ���� *Status+  
StatusText#
 [ �� -    ��Name$ 
RightPanel��$��1��$��3�� 	 
RightPanel#��[	 .+��� #��[ ���� $ SelectedHost.Name� �� StringFormat$ Имя: {0}���� #��[ ���� $ SelectedHost.IpAddress�$ IP-адрес: {0}���� #��[ ���� $ SelectedHost.MacAddress�$ MAC-адрес: {0}���� $ 
Horizontal=�.+��� #��[ $��Статус:��A� #��[BA����� *SelectedHost.Status+#
 [ �� #��[ ���� *SelectedHost.Status+#
 [ �� -   #��[B������ *SelectedHost.Status+ 
 PowerOnButtonText#
 [
 {��� *SelectedHost.Status+  StatusPowerBool#
 [ �� -	   #��[B��$#��Обновить статус��{��� *SelectedHost.Status+  StatusRefreshBool#
 [ �� -
   $��Удалить хост���� {��� *SelectedHost.Status+  
StatusBool#
 [ .+��� -   $	 RemoteButton��#��[B��$-��'Удалённое управление����   TextBox#��[ $ IBeam��$��Stretch=����� *TransferMessage+�� -   ���� *SelectedHost.IsTransfers+  TransferButtonText#
 [ �� ���� #��[ 
Логи���  �� $��75 ���  �� �� �� .+��� -   $��Экспорт логов��{��� *LogModel.IsWorkingWithFiles+#
 [ �� -   $��0��$��1��$��Импорт логов��{��� *LogModel.IsWorkingWithFiles+#
 [ �� -   $��0��$��2��$��Очистить логи��{��� *LogModel.IsWorkingWithFiles+#
 [ &   System.Windows.Controls.DataGrid  $��1��$��0��$��3��$��10q�   AutoGenerateColumns$ False��   HorizontalGridLinesBrush
 �����   RowBackground
 �����   AlternatingRowBackground
 �����   
IsReadOnly$
 True��   SelectionMode$ Single=�   SelectionUnit$ FullRow=�  Font#��[ ���� *LogModel.Logs+   Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn   Header$ ID��   Width5  /System.Windows.Controls.DataGridLengthConverter$ 75    SortDirection 	Ascending1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *Id+  $! Время создания��$	 225  �� *CreationTime+  $ Статус��$ 75  �� *Status+  $ Сообщение�� �� *Text+
   M S B A M L   `   `   `  ����G  CAdminProgram, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:AdminProgramAdminProgram  5      A%clr-namespace:AdminProgram.ConvertersAdminProgram.Converters  5      ;"clr-namespace:AdminProgram.HelpersAdminProgram.Helpers  5          AdminProgram.App   5      W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      ,localclr-namespace:AdminProgram     5      <
converters%clr-namespace:AdminProgram.Converters     5      6helpers"clr-namespace:AdminProgram.Helpers     5        �� 	Resources  5         
LightWhite  
GhostWhite  FlowerWhite  
SmokeWhite  
BeforeDark  Gray3   -AdminProgram.Converters.EllipseColorConverter  EllipseColor1   +AdminProgram.Converters.StatusTextConverter  
StatusText8   2AdminProgram.Converters.PowerOnButtonTextConverter  PowerOnButtonText5   /AdminProgram.Converters.ScanButtonTextConverter 	 ScanButtonText8   2AdminProgram.Converters.RefreshButtonTextConverter 
 RefreshButtonText9   3AdminProgram.Converters.TransferButtonTextConverter  TransferButtonText6   0AdminProgram.Converters.StatusPowerBoolConverter  StatusBoolPower1   +AdminProgram.Converters.StatusBoolConverter  
StatusBool3	   -AdminProgram.Converters.OppositeBoolConverter  OppositeBool7
   1AdminProgram.Converters.BoolToVisibilityConverter  BoolToVisibility  Font �� Property �� Value 
 Window �� BasedOn  	HostPanel �� 
TargetType  
RightPanel  TextBox  	SearchBox  SearchEmpty��%�	  &	        &	      &	 >     &	 ]     &	 |     &	 �     &	 �     &	 �     &	 �     &		 �     &	
     &	     &	 ,    &	 ?    &	 R    &	 e    &	 x    &	 9    7  7   7  &	 �    7  'A� �    &	 *    7  7  &	 �    7  7  7  &	 �    7  &	 �    7  7  '��     7  7  ��5
   
   #D7D7D7��66   ��5   
   #9DA1AA��66   ��5   
   #E6E6FA��67   ��5   
   #F5F5F5��66   ��5   
   #131313��66   ��5   
   #A0A0A4��60     5   
   6+     5   
   6)     5   
   60     5   
   6-     5   
   60     5   
   61     5   
   6.     5   
   6)   	  5   
   6+   
  5   
   6/   �� 5   
   .�5      ��  � ��$ 20(�61   �� 5       � ��$ Normal$�62   �� 5       � ��$ Arial, Sans-serif)�63   �� 5        � ��$ Normal&�64   5!      �� 5"   
   8   .�5#      ��  � ��8  63   �� 5$       � ��8  63   5%      �� 5&   
   8   .�5'      ��  � ��$ 10q�61   5(      �� 5)   
   " A�6   .�5*      ��  � ��$ 12��6&   �� 5+       � ��$ 12��6'   �� 5,       � ��
 �����6%   �� 5-       � ��$ 10q�6'   �� 5.       � ��$ 2��60   �� 5/       � ��
 �����6'   �� 50       � ��$ Center=�62   �� 51       � ��$ Center=�64   52      �� 53   
   8   .�54      ��  � ��8  66   �� 55       � ��$ 10q�62   �� 56       � y�$ 	Collapsed=�66   �� 57       � ��$ 1q�68   58      �� 5:   
   8   .�5;      ��  � ��8  63   �� 5<       � ��8  63   �� 5=       � ��$ 5q�60   �� 5>       � ��$ 10q�6/   �� 5?       � ��$ 1q�68   �� 5@       � ��$
 Left=�6<   �� 5A       � ��$ Center=�6:   5B      �� 5C   
   8   .�5D      �� % �@��HorizontalScrollBarVisibility$ Disabled=�6F   �� 5E      # �@��VerticalScrollBarVisibility$ Disabled=�6D   �� 5F       �@��	MaxLength$ 50��62   �� 5G       � ��$	 600��6.   5H      �� 5I   
   8   .�5J      ��  � ��8  61   �� 5K       � ��$ Italic$�60   �� 5L       � ��$ Поиск...��6.   �� 5M       � ��$ 5q�60   �� 5N       � ��$ 15q�6/   �� 5O       � ��$
 Left=�6<   �� 5P       � ��$ Center=�6:   5Q      �� 5S   
   " ��6   8   .�5T      ��  � ��8  6+   �� 5U       � ��$ 10q�6'   5V      5W      5X      
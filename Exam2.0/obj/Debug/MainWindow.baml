   M S B A M L   `   `   `  ����  Exam2.0     DownloadManager.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Download Manager��5   	   $	��600��6"   $	��800��6/   .��5      � $��10q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5	      $
��Auto �6   �� 5
      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $
��URL:��6   $��0��6   �� 5   
   -   5         ��Name$ txtUrl��5      $��0��6"   $��
50,0,150,5q�6/   $��25��6C   �� 5   
   $��Save to:��6   $��1��6#   �� 5   
   -   5        $ txtSavePath��5      $��1��6'   $��
50,0,150,5q�64   $��25��6H   �� 5   
   $��Threads:��6   $��2��6#   �� 5   
   -   5         ��Name$ 
cmbThreads��5      $��2��6'   $��
50,0,150,5q�64    �� SelectedIndex$ 0��6H   .@�5      �� $��1��6   �� 5      $��2��6   �� 5      $��3��6   �� 5      $��4��6   �� 5       $��5��6   5!      �� 5$   
   $��Tags (optional):��6   $��3��6+   �� 5%   
   -   5        $ txtTags��5%      $��3��6#   $��
50,0,150,5q�60   $��25��6D   �� 5(   
    �� Orientation$ 
Horizontal=�6   $��4��6.   $��Right=�6;   .+�5)      �� -   5         ��Name$ btnStart��5)      $��Start��6'   $��80��67   $��5q�6B   6M   �� 5*      -   5        $ btnPause��5*      $��Pause��6'   $��80��67   $��5q�6B   6M   �� 5+      -   5        $ btnStop��5+      $
��Stop��6&   $��80��65   $��5q�6@   6K   �� 5,      -   5        $ btnChooseLocation��5,      $��Choose Location��60   $	��120��6J   $��5q�6V   6a   5-      �� 50   
   -	   5         ��Name$ lvDownloads��50      $��5��6(   $��	0,10,0,10q�65    �� View 51      �� 52      .P�53      �� $	��URL��6%   	 �� Width$		 300��62   
 �� DisplayMemberBinding
 6>   �� *Url+�� 54      $��Progress��6%   $		 100��67   
 6C   �� *
Progress+ �� StringFormat$
 {0}%���� 55      $��Status��6%   $		 150��65   
 6A   �� *Status+56      58      �� 5;   
   -
   5        $ lvCompleted��5;      $��6��6(   $��	0,10,0,10q�65    5<      �� 5=      .P�5>      �� $
��File��6%   $		 300��63   
 6?   �� *
FileName+�� 5?      $
��Path��6%   $		 300��63   
 6?   �� *Path+�� 5@      $
��Tags��6%   $		 100��63   
 6?   �� *
TagsString+5A      5C      �� 5F   
   $ 
Horizontal=�6   $��7��6.   .+�5G      �� $��Search by tags:��6   �� 5H      -   5        $ txtSearchTags��5H      $��	10,0,10,0q�6-   $��25��6@   �� 5I      -   5        $ 	btnSearch��5I      $��Search��6(   $��80��69   $��5q�6D   6O   5J      5K      5L      
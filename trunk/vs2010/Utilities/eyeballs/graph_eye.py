import wx
import random

_MAJOR_GRID_LINES = 10
_MINOR_GRID_LINES = 10
_MAJOR_TICK_LENGTH = 8
_MINOR_TICK_LENGTH = 4
_MINOR_TICK_RATIO = 50
_DEFAULT_PANEL_SIZE = (300,200)
_LEFT_BORDER_PIXELS = 10
_RIGHT_BORDER_PIXELS = 10
_TOP_BORDER_PIXELS = 10
_BOTTOM_BORDER_PIXELS = 20
_LEGEND_X_OFFSET_PIXELS = 5
_LEGEND_Y_OFFSET_PIXELS = 10
_LEGEND_X_SPACING_PIXELS = 10

_STANDARD_COLORS = [ 
'AQUAMARINE' #,'BLACK'
,'BLUE','BLUE VIOLET','BROWN'#,'CADET BLUE'
,'CORAL','CORNFLOWER BLUE','CYAN','DARK GREY','DARK GREEN','DARK OLIVE GREEN'
,'DARK ORCHID','DARK SLATE BLUE','DARK SLATE GREY','DARK TURQUOISE','DIM GREY'
,'FIREBRICK','FOREST GREEN','GOLD','GOLDENROD','GREY','GREEN','GREEN YELLOW'
,'INDIAN RED','KHAKI','LIGHT BLUE','LIGHT GREY','LIGHT STEEL BLUE','LIME GREEN'
,'MAGENTA','MAROON','MEDIUM AQUAMARINE','MEDIUM BLUE','MEDIUM FOREST GREEN'
,'MEDIUM GOLDENROD','MEDIUM ORCHID','MEDIUM SEA GREEN','MEDIUM SLATE BLUE'
,'MEDIUM SPRING GREEN','MEDIUM TURQUOISE','MEDIUM VIOLET RED','MIDNIGHT BLUE'
,'NAVY','ORANGE','ORANGE RED','ORCHID','PALE GREEN','PINK','PLUM','PURPLE'
,'RED','SALMON','SEA GREEN','SIENNA','SKY BLUE','SLATE BLUE','SPRING GREEN'
,'STEEL BLUE','TAN','THISTLE','TURQUOISE','VIOLET','VIOLET RED'#,'WHEAT','WHITE'
,'YELLOW','YELLOW GREEN'
    ]

class GraphWindow():
    """stand alone graph window for graph panel"""
    def __init__(self, headers, data, close_handler=None):
        if headers is None:
            graph_title = 'eyeballs'
        else:
            graph_title = 'eyeballs - multiple data sets' if len(headers) > 1 else "eyeballs - x:'%s' vs y:'%s'" %(headers[0][0], headers[0][1])

        frame = wx.Frame(None, title=graph_title)
        self.graph = GraphPanel(frame, headers, data)
        self._close_handler = close_handler

        ib = wx.IconBundle()
        ib.AddIconFromFile("icon_eyeballs.ico", wx.BITMAP_TYPE_ANY)
        frame.SetIcons(ib)

        frame.Bind( wx.EVT_CLOSE, self.on_close)
        frame.Fit()
        frame.Show()
    def on_close(self, event):
        if self._close_handler is not None:
            self._close_handler(self.graph)
        event.Skip()

class GraphPanel(wx.Panel):
    """panel to graph data"""
    def __init__(self, parent, headers, data, *args, **kwargs):
        wx.Panel.__init__(self, parent, size=_DEFAULT_PANEL_SIZE, *args, **kwargs)

        self.headers = headers
        self.data = data 
        self.data_count = len(data)

        self.show_grid = True
        self.show_lines = True
        self.show_squares = True
        self.show_legend = True

        self.drag_rect = None
        self.left_down = False

        self.minor_tick_count = _MINOR_GRID_LINES

        unique_colors = ['STEEL BLUE', 'LIGHT BLUE', 'GREEN YELLOW', 'PINK']
        for i in range(2*self.data_count):
            unique_colors.append( self.pick_unique_color(unique_colors))

        self.background_brush = wx.Brush(unique_colors[0])
        self.grid_pen = wx.Pen(unique_colors[1], 1, wx.SOLID)
        self.drag_rect_pen = wx.Pen(unique_colors[2], 1, wx.DOT)
        self.text_color = unique_colors[3]      
        self.data_pens = [wx.Pen(unique_colors[4+i], 1, wx.SOLID) for i in range(self.data_count)]
        self.square_pens = [wx.Pen(unique_colors[5+i], 1, wx.SOLID) for i in range(self.data_count)]

        self.SetBackgroundStyle(wx.BG_STYLE_CUSTOM)
        self.Bind( wx.EVT_ERASE_BACKGROUND, self.on_erasebackground)
        self.Bind( wx.EVT_PAINT, self.on_paint)
        self.Bind( wx.EVT_SIZE, self.on_size)

        self.Bind( wx.EVT_LEFT_DOWN, self.on_leftbuttondown)
        self.Bind( wx.EVT_LEFT_UP, self.on_leftbuttonup)
        self.Bind( wx.EVT_RIGHT_DOWN, self.on_rightbuttondown)
        self.Bind( wx.EVT_RIGHT_UP, self.on_rightbuttonup)
        self.Bind( wx.EVT_MOTION, self.on_motion)

        self.ShowToolTipWindow(True)
        
        self.prepare_data() # initialize important data related constants
        self.on_size(None) # initialize backing buffer

    def ShowToolTipWindow(self, show):
        if show:
            # the initial string seems to define the maximum size of the tooltip window
            self.tooltip = wx.ToolTip('x\nyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy');
            self.tooltip.SetDelay(50)
            self.SetToolTip(self.tooltip)
        else:
            self.SetToolTip(None)
            self.tooltip = None       
    
    def pick_color(self):
        i = random.randint(0, len(_STANDARD_COLORS)-1)
        return _STANDARD_COLORS[i]

    def pick_unique_color(self, colors):
        color = None
        while color == None or color in colors:
            color = self.pick_color()
        return color

    def on_erasebackground(self, event):
        pass

    def on_size(self, event):
        size = self.GetClientSize()
        self._buffer = wx.EmptyBitmap(size[0], size[1])

        self.prepare_window_size()
        self.Refresh(False)

    def on_paint(self, event):
        dc = wx.AutoBufferedPaintDCFactory(self)
        self.PrepareDC(dc)
        dc.BeginDrawing()
        self.paint(dc)
        dc.EndDrawing()
 
    def paint(self, dc):
        dc.SetBackground( self.background_brush)
        dc.Clear()
        if self.show_grid:
            self.draw_grid(dc)
        self.draw_data(dc)

        if self.show_legend:
            self.draw_legend(dc)
    
        if self.drag_rect is not None:
            self.draw_drag_rect(dc)                 

    def drag_rect_to_rect(self):
        left = min(self.drag_rect[0], self.drag_rect[2])
        right = max(self.drag_rect[0], self.drag_rect[2])
        top = min(self.drag_rect[1], self.drag_rect[3])
        bottom = max(self.drag_rect[1], self.drag_rect[3])
        w = right - left
        h = bottom - top
        return (left, top, w, h)


    def draw_drag_rect(self, dc):
        rect = self.drag_rect_to_rect()

        dc.SetPen(self.drag_rect_pen)
        dc.SetBrush(wx.Brush("BLACK", wx.TRANSPARENT))
        dc.DrawRectangle(rect[0], rect[1], rect[2], rect[3])

    def draw_old_style_grid(self, dc):
        # old style grid is literally a grid.  kind of boring
        count = _GRID_LINES
        size = self.GetClientSize()
        left, right, top, bottom = (0.0, size[0], 0, size[1])
        width = right-left
        height = bottom-top
        w_step = width / float(count)
        h_step = height / float(count)
        dc.SetPen(self.grid_pen)
        for x in range(1, count):
            dc.DrawLinePoint( (left + w_step * x, top), (left + w_step * x, bottom))
            dc.DrawLinePoint( (left, top + h_step * x), (right, top + h_step * x))

    def draw_grid(self, dc):
        # new style grid is major and minor ticks on a rectangle around the margin
        major_tick_count = _MAJOR_GRID_LINES
        minor_tick_count = self.minor_tick_count
        major_tick_length = _MAJOR_TICK_LENGTH
        minor_tick_length = _MINOR_TICK_LENGTH
        size = self.GetClientSize()
        left_border = self.left_border
        right_border = self.right_border
        top_border = self.top_border
        bottom_border = self.bottom_border

        left, right, top, bottom = (0.0 + left_border, size[0] - right_border, 0 + top_border, size[1] - bottom_border) 
        width = right-left
        height = bottom-top

        w_step = width / float(major_tick_count)
        h_step = height / float(major_tick_count)

        wm_step = w_step / float(minor_tick_count)
        hm_step = h_step / float(minor_tick_count)
                
        dc.SetBrush(wx.Brush("BLACK", wx.TRANSPARENT))
        dc.SetPen(self.grid_pen)
        dc.DrawRectangle(left, top, width+1, height+1)

        for x in range(0, major_tick_count):
            left_0 = left + w_step * x
            top_0 = top + h_step * x
            dc.DrawLinePoint( (left_0, top), (left_0, top + major_tick_length))
            dc.DrawLinePoint( (left_0, bottom - major_tick_length), (left_0, bottom))
            dc.DrawLinePoint( (left, top_0), (left + major_tick_length, top_0))
            dc.DrawLinePoint( (right - major_tick_length, top_0), (right, top_0))
            for x1 in range(0, minor_tick_count):
                left_1 = left_0 + wm_step * x1
                top_1 = top_0 + hm_step * x1
                dc.DrawLinePoint( (left_1, top), (left_1, top + minor_tick_length))
                dc.DrawLinePoint( (left_1, bottom - minor_tick_length), (left_1, bottom))
                dc.DrawLinePoint( (left, top_1), (left + minor_tick_length, top_1))
                dc.DrawLinePoint( (right - minor_tick_length, top_1), (right, top_1))
                                    
    def prepare_data(self):
        self.data_len = [len(self.data[i]) for i in range(self.data_count)]

        self.x_data = [[x[0] for x in self.data[i]] for i in range(self.data_count)]
        self.y_data = [[x[1] for x in self.data[i]] for i in range(self.data_count)]

        all_x_data = [j for i in self.x_data for j in i]
        all_y_data = [j for i in self.y_data for j in i]

        self.biggest_y = reduce(lambda _1, _2: max(_1,_2), all_y_data)
        self.smallest_y = reduce(lambda _1, _2: min(_1,_2), all_y_data)

        self.biggest_x = reduce(lambda _1, _2: max(_1,_2), all_x_data)
        self.smallest_x = reduce(lambda _1, _2: min(_1,_2), all_x_data)

    def prepare_window_size(self):
        biggest_y = self.biggest_y
        biggest_x = self.biggest_x
        smallest_y = self.smallest_y
        smallest_x = self.smallest_x

        # window dimensions
        size = self.GetClientSize()
        self.left, self.right, top, bottom = (0.0, size[0], 0, size[1])
        self.width = self.right-self.left
        self.height = bottom-top

        # w scale is % of horizontal screen to use (ie how much border to leave)
        #w_scale = 0.95
        #w_scaled = w_scale * self.width
        #self.w_border = (self.width - w_scaled) / 2.0
        self.left_border = _LEFT_BORDER_PIXELS
        self.right_border = _RIGHT_BORDER_PIXELS
        w_scaled = self.width - (self.left_border + self.right_border)

        # w step is how many pixels per x unit
        self.w_step = w_scaled / float(biggest_x-smallest_x) if (biggest_x-smallest_x) != 0.0 else 0.0

        # h_scale is % of vertical screen to use (ie how much border to leave)
        #h_scale = 0.85
        #h_scaled = h_scale * self.height
        #self.h_border = (self.height - h_scaled) / 2.0
        self.top_border = _TOP_BORDER_PIXELS
        self.bottom_border = _BOTTOM_BORDER_PIXELS
        h_scaled = self.height - (self.top_border + self.bottom_border)

        # h_step is how many pixels per y unit
        self.h_step = h_scaled / float(biggest_y-smallest_y) if (biggest_y-smallest_y) != 0.0 else 0.0


        grid_width = size[0] - (self.left_border + self.right_border)
        self.minor_tick_count = int(min(_MINOR_GRID_LINES, grid_width / _MINOR_TICK_RATIO))


    def draw_data(self, dc):
        if self.data is None:
            return

        for j in range(self.data_count):

            x_data = self.x_data[j]
            y_data = self.y_data[j]
            count = self.data_len[j]
            data_pen = self.data_pens[j]
            square_pen = self.square_pens[j]

            biggest_y = self.biggest_y
            smallest_y = self.smallest_y
            biggest_x = self.biggest_x
            smallest_x = self.smallest_x
            

            left = self.left
            right = self.right
            width = self.width
            height = self.height
            left_border = self.left_border
            right_border = self.right_border
            w_step = self.w_step
            top_border = self.top_border
            bottom_border = self.bottom_border
            h_step = self.h_step

            dc.SetPen(data_pen)

            # special case where biggest_y == smallest_y is a straight line across at a fixed value
            # draw this case at the bottom if its zero, otherwise at the top
            if biggest_y == smallest_y:
                h_point = height - bottom_border if biggest_y == 0.0 else top_border
                dc.DrawLinePoint((self.left, h_point), (self.right, h_point))
                return

            # normalize points so that smallest y value is at bottom, largest is at top
            #
            # norm = (data - smallest_y) * height / (biggest_y-smallest_y)
            # h_next & h_previous order is inverted since screen zero is upperleft
            #
            x_last = left + left_border + (x_data[0] - smallest_x) * w_step
            y_last = height - (bottom_border + (y_data[0] - smallest_y) * h_step)
            dc.DrawPoint(x_last, y_last)

            if self.show_squares:
                for i in range(0, count):
                    x = left + left_border + (x_data[i] - smallest_x) * w_step
                    y = height - (bottom_border + (y_data[i] - smallest_y) * h_step)
                    dc.SetPen(square_pen)
                    dc.DrawRectangle(x-1, y-1, 3, 3)
                    dc.SetPen(data_pen)

            for i in range(1, count):
                x = left + left_border + (x_data[i] - smallest_x) * w_step
                y = height - (bottom_border + (y_data[i] - smallest_y) * h_step)
                if self.show_lines:
                    dc.DrawLinePoint((x_last, y_last), (x, y))
                else:
                    dc.DrawPoint(x, y)

                x_last = x
                y_last = y

    def draw_legend(self, dc):
        # draw the header legend starting in the upper left corner, working our way down
        # don't draw it if the window is too small

        x = _LEGEND_X_OFFSET_PIXELS
        y = self.height - _LEGEND_Y_OFFSET_PIXELS
        line_len = 20
        text_height = 20

        font = wx.Font(8, wx.FONTFAMILY_SWISS, wx.FONTSTYLE_NORMAL, wx.FONTWEIGHT_NORMAL, False, 'Calibri', wx.FONTENCODING_SYSTEM)
        dc.SetFont(font)
        
        for i in range(self.data_count):
            if self.show_squares:
                dc.SetPen(self.square_pens[i])
                dc.DrawRectangle(x+line_len/2-1, y-1, 3, 3)            
            dc.SetPen(self.data_pens[i])
            dc.DrawLinePoint((x,y), (x+line_len, y))
            dc.SetTextForeground(self.text_color)
            x += 1.25 * line_len
            dc.DrawText(self.headers[i][1], x, y-text_height*.25)

            size = dc.GetTextExtent(self.headers[i][1])
            x += size[0] + _LEGEND_X_SPACING_PIXELS

    def on_leftbuttondown(self, event):
        self.left_down = True
        self.start_dragging(event)

    def on_rightbuttondown(self, event):
        self.start_dragging(event)

    def start_dragging(self, event):
        event.Skip()
        self.CaptureMouse()
        self.drag_rect = (event.m_x, event.m_y, event.m_x, event.m_y)

        self.ShowToolTipWindow(False)

    def on_leftbuttonup(self, event):
        self.set_zoom_rect()
        self.end_dragging(event)
        self.left_down = False

    def on_rightbuttonup(self, event):
        self.end_dragging(event)
        self.prepare_data()
        self.prepare_window_size()

    def end_dragging(self, event):
        event.Skip()
        if self.HasCapture():
            self.ReleaseMouse()
        self.drag_rect = None
        self.Refresh(False)

        self.ShowToolTipWindow(True)

    def on_motion(self, event):
        event.Skip()
        if self.drag_rect is not None:
            self.drag_rect = (self.drag_rect[0], self.drag_rect[1], event.m_x, event.m_y)
            self.Refresh(False)
            return
        #otherwise, display the data coordinates
        if self.tooltip is not None:
            # if there's only 1 data set, tooltip uses header label, otherwise tooltip uses 'x' & 'y'
            legend = ('x', 'y') if self.data_count > 1 else (self.headers[0][0], self.headers[0][1])
            point = self.point_to_data_space(event.m_x, event.m_y)
            self.tooltip.SetTip('%s: %.4f\n%s: %.4f' % (legend[0], point[0], legend[1], point[1]))

    def set_zoom_rect(self):
        if self.drag_rect is None:
            return

        x0 = min(self.drag_rect[0], self.drag_rect[2])
        x1 = max(self.drag_rect[0], self.drag_rect[2])
        y0 = min(self.drag_rect[1], self.drag_rect[3])
        y1 = max(self.drag_rect[1], self.drag_rect[3])

        # bail if we didn't really drag a rectangle -- avoid left clicking zooming to infinity
        if x1-x0 < 10:
            return

        s_x, s_y = self.point_to_data_space(x0, y1)
        b_x, b_y = self.point_to_data_space(x1, y0)

        self.smallest_x, self.biggest_x = s_x, b_x
        self.smallest_y, self.biggest_y = s_y, b_y        

        self.prepare_window_size()

    def point_to_data_space(self, px, py):
    
        x = self.smallest_x + ((px - self.left - self.left_border) / self.w_step) if self.w_step != 0.0 else 0.0
        y = self.smallest_y + ((self.height - py - self.bottom_border) / self.h_step) if self.h_step != 0.0 else 0.0

        return (x, y)
 

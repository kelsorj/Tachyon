// SetupFrontEndDlg.cpp : implementation file
//

#include "stdafx.h"
#include "SetupFrontEnd.h"
#include "SetupFrontEndDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// Dialog Data
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
END_MESSAGE_MAP()


// CSetupFrontEndDlg dialog



CSetupFrontEndDlg::CSetupFrontEndDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CSetupFrontEndDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CSetupFrontEndDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CSetupFrontEndDlg, CDialog)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_WM_CREATE()
	ON_WM_CTLCOLOR()
	ON_BN_CLICKED(IDC_BUTTON_INSTALLAQ3, OnBnClickedButtonInstallaq3)
	ON_BN_CLICKED(IDC_BUTTON_INSTALLIE, OnBnClickedButtonInstallie)
	ON_BN_CLICKED(IDC_BUTTON_INSTALLDOTNET, OnBnClickedButtonInstalldotnet)
END_MESSAGE_MAP()


// CSetupFrontEndDlg message handlers

BOOL CSetupFrontEndDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	
	//set dialog background color
	m_brush.CreateSolidBrush(RGB(120, 157, 227));
	
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CSetupFrontEndDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CSetupFrontEndDlg::OnPaint() 
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CSetupFrontEndDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

int CSetupFrontEndDlg::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (CDialog::OnCreate(lpCreateStruct) == -1)
		return -1;

	// TODO:  Add your specialized creation code here

	return 0;
}

HBRUSH CSetupFrontEndDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialog::OnCtlColor(pDC, pWnd, nCtlColor);

	switch (nCtlColor)
	{
		//Edit controls need white background and black text
		//Note the 'return hbr' which is needed to draw the Edit
		//control's internal background (as opposed to text background)
	case CTLCOLOR_EDIT:
		pDC->SetTextColor(RGB(255,0,0));
		pDC->SetBkColor(RGB(255,255,255));
		return hbr;
		//Static controls need black text and same background as m_brush
	case CTLCOLOR_STATIC:
		LOGBRUSH logbrush;
		m_brush.GetLogBrush( &logbrush );
		pDC->SetTextColor(RGB(255,255,255));
		pDC->SetBkColor(logbrush.lbColor);
		return m_brush;
	case CTLCOLOR_BTN:
		pDC->SetTextColor(RGB(255,255,255));
		pDC->SetBkColor(RGB(255,128,128));
		return m_brush;
		//For listboxes, scrollbars, buttons, messageboxes and dialogs,
		//use the new brush (m_brush)
	case CTLCOLOR_LISTBOX:
	case CTLCOLOR_SCROLLBAR:
	case CTLCOLOR_MSGBOX:
	case CTLCOLOR_DLG:
		return m_brush;
		//This shouldn't occurr since we took all the cases, but
		//JUST IN CASE, return the new brush
	default:
		return m_brush;
	} 
}

#include <process.h>

void CSetupFrontEndDlg::OnBnClickedButtonInstallaq3()
{
	spawnlp(P_NOWAIT, "dw4/setup.exe", "dw4/setup.exe", NULL);
}

void CSetupFrontEndDlg::OnBnClickedButtonInstallie()
{
	spawnlp(P_NOWAIT, "ie/ie6setup.exe", "ie/ie6setup.exe", NULL);
}

void CSetupFrontEndDlg::OnBnClickedButtonInstalldotnet()
{
	spawnlp(P_NOWAIT, "dotnet/dotnetfx.exe", "dotnet/dotnetfx.exe", NULL);
}

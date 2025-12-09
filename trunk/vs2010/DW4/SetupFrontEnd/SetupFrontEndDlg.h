// SetupFrontEndDlg.h : header file
//

#pragma once


// CSetupFrontEndDlg dialog
class CSetupFrontEndDlg : public CDialog
{
// Construction
public:
	CSetupFrontEndDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_SETUPFRONTEND_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
protected:
	CBrush m_brush;
public:
	afx_msg void OnBnClickedButtonInstallaq3();
	afx_msg void OnBnClickedButtonInstallie();
	afx_msg void OnBnClickedButtonInstalldotnet();
};

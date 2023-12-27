select [job_mst].job, [job_mst].suffix, [job_mst].job_date,[job_mst].ord_type, [job_mst].item, [job_mst].stat,[jobroute_mst].[oper_num]
      ,[jobroute_mst].[wc]
      ,[jobroute_mst].[complete]
	  ,[jobroute_mst].[bflush_type]
	  ,[jobroute_mst].[cntrl_point]
      ,[jobroute_mst].[qty_scrapped]
      ,[jobroute_mst].[qty_received]
      ,[jobroute_mst].[qty_moved]
      ,[jobroute_mst].[qty_complete]
	  ,[tbJobTransMain].[_qty_complete] as "main_comp"
	  ,[tbJobTransMain].[_qty_scrapped] as "main_scra"
	  ,[tbJobTransMain].[_tansnum]
	  ,[jrt_sch_mst].[sched_drv]
	  ,[jrt_sch_mst].run_mch_hrs
	  ,[jrt_sch_mst].run_lbr_hrs
	  ,[jobroute_mst].fovhd_rate_mch
	  ,[jobroute_mst].fixovhd_rate
	  ,[jobroute_mst].vovhd_rate_mch
	  ,[jobroute_mst].varovhd_rate
	  ,maxtable.last_oper
	  ,[item_mst].Uf_FIFO_FEFO
	  ,[tbJobTransMain]._nextoper_num
	  ,[tbJobTransMain]._start_time
	  ,[tbJobTransMain]._end_time
	  ,[item_mst].shelf_life
	  ,[jobroute_mst].run_rate_lbr
	  ,[jobroute_mst].fixovhd_t_mch
	  ,[jobroute_mst].fixovhd_t_lbr
	  ,[jobroute_mst].varovhd_t_mch
	  ,[jobroute_mst].varovhd_t_lbr
	  ,[job_mst].revision
	  ,[jobroute_mst].run_hrs_t_mch
	  ,[jobroute_mst].run_hrs_t_lbr
	  ,[jobroute_mst].run_cost_t_lbr

	  --,cycletime._actualCT --add actualCT, for Yixin tooling
	  --,ahrs.a_hrs AS a_hrs_CSI
	  ,([jobroute_mst].run_hrs_t_mch+[jobroute_mst].run_hrs_t_lbr) AS a_hrs_CSI
	  ,ROUND([tbJobTransMain].a_hrs / 60,3) AS a_hrs_EWIP
	  ,[poitem_mst].ref_num AS machining_lot

    FROM [YixinTest].[dbo].[job_mst]       
	inner join  [YixinTest].[dbo].[jobroute_mst]
	on [job_mst].job = [jobroute_mst].job and  [job_mst].suffix = [jobroute_mst].suffix
	inner join [YXERPSVR].[eWIP_YX_Test].[dbo].[tbJobTransMain]
	on [job_mst].job = [tbJobTransMain]._job and [jobroute_mst].[oper_num] = [tbJobTransMain]._oper_num
	left join [YixinTest].[dbo].[jrt_sch_mst]
	on  [job_mst].[job] = [jrt_sch_mst].[job] and [jrt_sch_mst].[oper_num] = [jobroute_mst].[oper_num]
	left join (select [jobroute_mst].job,max([jobroute_mst].oper_num) as last_oper From [YixinTest].[dbo].[jobroute_mst] Group By [jobroute_mst].job) as maxtable
	on [job_mst].job = maxtable.job
	left join [YixinTest].[dbo].[item_mst]
	on [job_mst].item = [item_mst].item
	left join [YXERPSVR].[eWIP_YX_Test].[dbo].[tbJobRoute]
	on [jobroute_mst].job = [tbJobRoute]._job and [jobroute_mst].oper_num = [tbJobRoute]._operationNo
	left join [YixinLive].[dbo].co_mst on job_mst.ord_num=co_mst.co_num
	left join [AMTLive].[dbo].poitem_mst on co_mst.cust_po=poitem_mst.po_num and job_mst.ord_line=poitem_mst.po_line and job_mst.ord_release = poitem_mst.po_release
	----add actualCT, for Yixin tooling
	--left join (select _job,_oper_num,SUM(_actualCT)as _actualCT from [YXERPSVR].[eWIP_YX_Test].[dbo].[tbJobTrans] Group By _job,_oper_num) as cycletime
	--on [job_mst].job = cycletime._job and [jobroute_mst].[oper_num] = cycletime._oper_num
	----deduct a_hrs from _actualCT to get reworked cycletime
	--left join (select job,oper_num,SUM(a_hrs)as a_hrs from yixintest.dbo.jobtran_mst  Group By job,oper_num) as ahrs
	--on [job_mst].job = ahrs.job and [jobroute_mst].[oper_num] = ahrs.oper_num

	where [job_mst].type='J' and [job_mst].stat<>'C' and [jobroute_mst].complete = 0 and [jobroute_mst].[bflush_type] = 'N' and [jobroute_mst].[cntrl_point] = 1 